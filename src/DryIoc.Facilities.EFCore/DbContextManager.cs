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
using DryIoc.Facilities.EFCore.Errors;
using DryIoc.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	/// <summary>
	/// 	The DbContext manager is an object wrapper around the "real" manager which is managed
	/// 	by a custom per-transaction lifestyle. If you wish to implement your own manager, you can
	/// 	pass a function to this object at construction time and replace the built-in DbContext manager.
	/// </summary>
	public class DbContextManager : IDbContextManager
	{
		private readonly Func<DbContext> getDbContext;
		private readonly ITransactionManager transactionManager;
		private readonly IDbContextStore dbContextStore;
		private readonly TransactionCommitAction commitAction;

		/// <summary>
		/// 	Constructor.
		/// </summary>
		/// <param name="getDbContext"></param>
		/// <param name="transactionManager"></param>
		/// <param name="dbContextStore"></param>
		/// <param name="commitAction"></param>
		public DbContextManager(Func<DbContext> getDbContext, ITransactionManager transactionManager, IDbContextStore dbContextStore, TransactionCommitAction commitAction)
		{
			Contract.Requires(getDbContext != null);
			Contract.Ensures(this.getDbContext != null);

			this.getDbContext = getDbContext;
			this.transactionManager = transactionManager;
			this.dbContextStore = dbContextStore;
			this.commitAction = commitAction;
		}

		DbContext IDbContextManager.OpenDbContext()
		{
			Maybe<ITransaction> transaction = ObtainCurrentTransaction();

			//This is a new transaction or no transaction is required
			if (!transaction.HasValue)
			{
				var dbContext = getDbContext();

				if (dbContext == null)
					throw new EFCoreFacilityException(
						"The Func<DbContext> passed to DbContextManager returned a null dbContext. Verify your registration.");

				return dbContext;
			}
			else
			{
				var dbContext = GetStoredDbContext();

				//There is an active transaction but no DbContext is created yet
				if (dbContext == null)
				{
					dbContext = getDbContext();

					if (dbContext == null)
						throw new EFCoreFacilityException(
							"The Func<DbContext> passed to DbContextManager returned a null dbContext. Verify your registration.");

					//Store the DbContext so I can reused
					StoreDbContext(dbContext);

					//Attach to the TransactionEvent so I can clean the callcontext
					transaction.Value.Inner.TransactionCompleted += Inner_TransactionCompleted;
					
					return dbContext;
				}
				else
				{
					return dbContext;
				}
			}
		}

		/// <summary>
		/// Clear the CallContext when the transaction ends
		/// </summary>
		/// <param name="sender">Just event stuff</param>
		/// <param name="e">Just event stuff</param>
		void Inner_TransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
		{
			var dbContext = dbContextStore.GetData();

			switch (commitAction)
			{
				case TransactionCommitAction.Nothing:
					break;
				case TransactionCommitAction.Dispose:
					dbContext.Dispose();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			ClearStoredDbContext();
		}

		/// <summary>
		/// Gets the current transaction from de AutoTx facility via an ITransactionManager
		/// </summary>
		/// <returns>The current transaction</returns>
		private Maybe<ITransaction> ObtainCurrentTransaction()
		{
			return transactionManager.CurrentTransaction;
		}

		/// <summary>
		/// Stores a dbContext in the CallContext
		/// </summary>
		/// <param name="dbContext">current dbContext</param>
		private void StoreDbContext(DbContext dbContext)
		{
			dbContextStore.SetData(dbContext);
		}

		/// <summary>
		/// Retrieves the dbContext stored in the callcontext
		/// </summary>
		/// <returns></returns>
		private DbContext GetStoredDbContext()
		{
			return dbContextStore.GetData();
		}

		/// <summary>
		/// Removes the dbContext stored in the callcontext
		/// </summary>
		/// <returns></returns>
		private void ClearStoredDbContext()
		{
			dbContextStore.ClearData();
		}
	}
}