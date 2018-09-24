// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.Contracts;
using System.Transactions;
using DryIoc.Facilities.AutoTx;
using DryIoc.Facilities.NHibernate.Errors;
using DryIoc.Facilities.NHibernate.UnitOfWork;
using DryIoc.Transactions;
using Microsoft.Extensions.Logging;
using NHibernate;

namespace DryIoc.Facilities.NHibernate
{
	using ITransaction = DryIoc.Transactions.ITransaction;

	/// <summary>
	/// 	The session manager is an object wrapper around the "real" manager which is managed
	/// 	by a custom per-transaction lifestyle. If you wish to implement your own manager, you can
	/// 	pass a function to this object at construction time and replace the built-in session manager.
	/// </summary>
	public class SessionManager : ISessionManager
	{
		private readonly Func<ISession> _GetSession;
		private readonly ITransactionManager _TransactionManager;
		private readonly ISessionStore _SessionStore;
		private readonly AutoTxOptions _AutoTxOptions;
		private readonly ILogger _Logger;

		/// <summary>
		/// 	Constructor.
		/// </summary>
		/// <param name = "getSession"></param>
		/// <param name="transactionManager"></param>
		/// <param name="sessionStore"></param>
		/// <param name="autoTxOptions"></param>
		/// <param name="logger"></param>
		public SessionManager(Func<ISession> getSession, ITransactionManager transactionManager, ISessionStore sessionStore, AutoTxOptions autoTxOptions, ILogger logger)
		{
			Contract.Requires(getSession != null);
			Contract.Ensures(this._GetSession != null);

			_GetSession = getSession;
			_TransactionManager = transactionManager;
			_SessionStore = sessionStore;
			_AutoTxOptions = autoTxOptions;
			_Logger = logger;
		}

		ISession ISessionManager.OpenSession()
		{
			Maybe<ITransaction> transaction = ObtainCurrentTransaction();

			//This is a new transaction or no transaction is required
			if (!transaction.HasValue)
			{
				var session = _GetSession();

				if (session == null)
					throw new NHibernateFacilityException(
						"The Func<ISession> passed to SessionManager returned a null session. Verify your registration.");

				return session;
			}
			else
			{
				var session = GetStoredSession();

				//There is an active transaction but no session is created yet
				if (session == null)
				{
					session = _GetSession();

					if (session == null)
						throw new NHibernateFacilityException(
							"The Func<ISession> passed to SessionManager returned a null session. Verify your registration.");

					//Store the session so I can reused
					StoreSession(session);

					//Attach to the TransactionEvent so I can clean the callcontext
					transaction.Value.Inner.TransactionCompleted += Inner_TransactionCompleted;
					
					return session;
				}
				else
				{
					return session;
				}
			}
		}

		/// <summary>
		/// Clear the CallContext when the transaction ends
		/// </summary>
		/// <param name="sender">Just event stuff</param>
		/// <param name="e">Just event stuff</param>
		void Inner_TransactionCompleted(object sender, TransactionEventArgs e)
		{
			try
			{
				FinishStoredSession(e.Transaction.TransactionInformation.Status);
			}
			catch (HibernateException exception)
			{
				_Logger.LogWarning(exception, "Error in the O-R persistence layer in NHibernate");
				throw;
			}
		}

		/// <summary>
		/// Gets the current transaction from de AutoTx facility via an ITransactionManager
		/// </summary>
		/// <returns>The current transaction</returns>
		private Maybe<ITransaction> ObtainCurrentTransaction()
		{
			return _TransactionManager.CurrentTransaction;
		}

		private IUnitOfWork CreateUnitOfWork(ISession session)
		{
			switch (_AutoTxOptions.AmbientTransaction)
			{
				case AmbientTransactionOption.Enabled:
					return new NHibernateImplictUnitOfWork(session);
				case AmbientTransactionOption.Disabled:
					return new NHibernateExplicitUnitOfWork(session);
				default:
					throw new ArgumentOutOfRangeException($"Unknown value: {_AutoTxOptions.AmbientTransaction}", (Exception) null);
			}
		}

		/// <summary>
		/// Stores a session in the CallContext
		/// </summary>
		/// <param name="session">current session</param>
		private void StoreSession(ISession session)
		{
			var unitOfWork = CreateUnitOfWork(session);
			_SessionStore.SetData(unitOfWork);
		}

		/// <summary>
		/// Retrieves the session stored in the callcontext
		/// </summary>
		/// <returns></returns>
		private ISession GetStoredSession()
		{
			return _SessionStore.GetData()?.CurrentSession;
		}

		/// <summary>
		/// Removes the session stored in the callcontext
		/// </summary>
		/// <param name="transactionStatus"></param>
		/// <returns></returns>
		private void FinishStoredSession(TransactionStatus transactionStatus)
		{
			using (var unitOfWork = _SessionStore.GetAndClearData())
			{
				switch (transactionStatus)
				{
					case TransactionStatus.Committed:
						unitOfWork.Commit();
						break;
					case TransactionStatus.Aborted:
					case TransactionStatus.Active:
					case TransactionStatus.InDoubt:
						unitOfWork.Rollback();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(transactionStatus), transactionStatus, null);
				}
			}
		}

	}
}