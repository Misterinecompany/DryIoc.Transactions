#region license

// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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

#endregion

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Castle.DynamicProxy;
using Castle.Transactions;
using Castle.Transactions.Internal;
using DryIoc;
using DryIoc.Facilities.AutoTx.Abstraction;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Transactions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionException = Castle.Transactions.TransactionException;
using TransactionManager = Castle.Transactions.TransactionManager;

namespace Castle.Facilities.AutoTx
{
	internal class TransactionInterceptor : IInterceptor
	{
		private enum InterceptorState
		{
			Constructed,
			Initialized,
			Active
		}

		private InterceptorState _State;
		private readonly IContainer _Container;
		private readonly ITransactionMetaInfoStore _Store;
		private Maybe<TransactionalClassMetaInfo> _MetaInfo;
		private ILoggerFactory _LoggerFactory = NullLoggerFactory.Instance;
		private ILogger _Logger = NullLogger.Instance;

		public ILogger Logger
		{
			get { return _Logger; }
			set { _Logger = value; }
		}

		public TransactionInterceptor(IContainer container, ITransactionMetaInfoStore store, ServiceRequestInfo serviceRequestInfo)
		{
			Contract.Requires(container != null, "container must be non null");
			Contract.Requires(store != null, "store must be non null");
			Contract.Ensures(_State == InterceptorState.Constructed);

			_Logger.LogDebug("created transaction interceptor");

			_Container = container;
			_Store = store;
			_State = InterceptorState.Constructed;

			SetInterceptedComponentModel(serviceRequestInfo);
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(_State != InterceptorState.Initialized || _MetaInfo != null);
		}

		void IInterceptor.Intercept(IInvocation invocation)
		{
			Contract.Assume(_State == InterceptorState.Active || _State == InterceptorState.Initialized);
			Contract.Assume(invocation != null);

			var txManagerC = _Container.Resolve<TransactionManager>();
			ITransactionManager txManager = txManagerC;

			var mTxMethod = _MetaInfo.Do(x => x.AsTransactional(invocation.MethodInvocationTarget ?? invocation.Method));

			var mTxData = mTxMethod.Do(x => txManager.CreateTransaction(x));

			_State = InterceptorState.Active;

			try
			{
				if (!mTxData.HasValue)
				{
					if (mTxMethod.HasValue && mTxMethod.Value.Mode == TransactionScopeOption.Suppress)
					{
						_Logger.LogInformation("supressing ambient transaction");
						if (_Logger.IsEnabled(LogLevel.Information))
							_Logger.LogInformation("supressing ambient transaction");

						using (new TxScope(null, _LoggerFactory.CreateChildLogger("TxScope", GetType())))
							invocation.Proceed();
					}
					else invocation.Proceed();

					return;
				}

				var transaction = mTxData.Value.Transaction;
				Contract.Assume(transaction.State == TransactionState.Active,
				                "from post-condition of ITransactionManager CreateTransaction in the (HasValue -> ...)-case");

				if (mTxData.Value.ShouldFork)
				{
					var task = ForkCase(invocation, mTxData.Value);
					txManagerC.EnlistDependentTask(task);
				}
				else SynchronizedCase(invocation, transaction);
			}
			finally
			{
				_Container.Release(txManagerC);
			}
		}

		private void SynchronizedCase(IInvocation invocation, ITransaction transaction)
		{
			Contract.Requires(transaction.State == TransactionState.Active);

			using (new TxScope(transaction.Inner, _LoggerFactory.CreateChildLogger("TxScope", GetType())))
			{
				var localIdentifier = transaction.LocalIdentifier;

				try
				{
					invocation.Proceed();

					if (transaction.State == TransactionState.Active)
						transaction.Complete();
					else if (_Logger.IsEnabled(LogLevel.Warning))
						_Logger.LogWarning(
							"transaction was in state {0}, so it cannot be completed. the 'consumer' method, so to speak, might have rolled it back.",
							transaction.State);
				}
				catch (TransactionAbortedException)
				{
					// if we have aborted the transaction, we both warn and re-throw the exception
					if (_Logger.IsEnabled(LogLevel.Warning))
						_Logger.LogWarning("transaction aborted - synchronized case, tx#{0}", localIdentifier);

					throw;
				}
				catch (TransactionException ex)
				{
					if (_Logger.IsEnabled(LogLevel.Critical))
						_Logger.LogCritical("internal error in transaction system - synchronized case", ex);

					throw;
				}
				catch (AggregateException ex)
				{
					if (_Logger.IsEnabled(LogLevel.Warning))
						_Logger.LogWarning("one or more dependent transactions failed, re-throwing exceptions!", ex);

					throw;
				}
				catch (Exception)
				{
					_Logger.LogError("caught exception, transaction will roll back - synchronized case - tx#{0}",
								  localIdentifier);

					// the transaction rolls back itself on exceptions, so just throw it
					throw;
				}
				finally
				{
					_Logger.LogDebug("dispoing transaction - synchronized case - tx#{0}", localIdentifier);
					transaction.Dispose();
				}
			}
		}

		/// <summary>
		/// For ordering interleaving of threads during testing!
		/// </summary>
		internal static ManualResetEvent Finally;

		private Task ForkCase(IInvocation invocation, ICreatedTransaction txData)
		{
			Contract.Requires(txData.Transaction.State == TransactionState.Active);
			Contract.Ensures(Contract.Result<Task>() != null);
			Contract.Assume(txData.Transaction.Inner is DependentTransaction);

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("fork case");

			return Task.Factory.StartNew(t =>
			{
				var hasException = false;
				var tuple = (Tuple<IInvocation, ICreatedTransaction, string>)t;
				var dependent = tuple.Item2.Transaction.Inner as DependentTransaction;

				using (tuple.Item2.GetForkScope())
				{
					try
					{
						if (_Logger.IsEnabled(LogLevel.Debug))
							_Logger.LogDebug($"calling proceed on tx#{tuple.Item3}");

						using (var ts = new TransactionScope(dependent))
						{
							tuple.Item1.Proceed();

							if (_Logger.IsEnabled(LogLevel.Debug))
								_Logger.LogDebug($"calling complete on TransactionScope for tx#{tuple.Item3}");

							ts.Complete();
						}
					}
					catch (TransactionAbortedException ex)
					{
						// if we have aborted the transaction, we both warn and re-throw the exception
						hasException = true;

						if (_Logger.IsEnabled(LogLevel.Warning))
							_Logger.LogWarning("transaction aborted", ex);

						throw new TransactionAbortedException(
							"Parallel/forked transaction aborted! See inner exception for details.", ex);
					}
					catch (Exception)
					{
						hasException = true;
						throw;
					}
					finally
					{
						if (_Logger.IsEnabled(LogLevel.Debug))
							_Logger.LogDebug("in finally-clause, completing dependent if it didn't throw exception");

						if (!hasException)
							dependent.Complete();

						if (Finally != null) 
							Finally.Set();

						// See footnote at end of file
					}
				}
			}, Tuple.Create(invocation, txData, txData.Transaction.LocalIdentifier));
		}

		private void SetInterceptedComponentModel(ServiceRequestInfo target)
		{
			Contract.Ensures(_MetaInfo != null);
			Contract.Assume(target.ImplementationType != null);
			_MetaInfo = _Store.GetMetaFromType(target.ImplementationType);
			_State = InterceptorState.Initialized;
		}
	}

	// * In their code: http://msdn.microsoft.com/en-us/library/ms229976%28v=vs.90%29.aspx#Y17
	//
	// MSDN article is wrong; transaction is committed twice now:
	//                      System.InvalidOperationException was unhandled by user code
	//Message=The operation is not valid for the current state of the enlistment.
	//Source=System.Transactions
	//StackTrace:
	//     at System.Transactions.EnlistmentState.ForceRollback(InternalEnlistment enlistment, Exception e)
	//     at System.Transactions.PreparingEnlistment.ForceRollback(Exception e)
	//     at NHibernate.Transaction.AdoNetWithDistributedTransactionFactory.DistributedTransactionContext.System.Transactions.IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment) in d:\CSharp\NH\NH\nhibernate\src\NHibernate\Transaction\AdoNetWithDistributedTransactionFactory.cs:line 129
	//     at System.Transactions.VolatileEnlistmentPreparing.EnterState(InternalEnlistment enlistment)
	//     at System.Transactions.VolatileEnlistmentActive.ChangeStatePreparing(InternalEnlistment enlistment)
	//     at System.Transactions.TransactionStatePhase0.Phase0VolatilePrepareDone(InternalTransaction tx)
	//     at System.Transactions.EnlistableStates.CompleteBlockingClone(InternalTransaction tx)
	//     at System.Transactions.DependentTransaction.Complete()
	//     at Castle.Facilities.AutoTx.TransactionInterceptor.<ForkCase>b__4(Object t) in f:\code\castle\Castle.Services.Transaction\src\Castle.Facilities.AutoTx\TransactionInterceptor.cs:line 144
	//     at System.Threading.Tasks.Task.InnerInvoke()
	//     at System.Threading.Tasks.Task.Execute()
	//InnerException: 
	//dependent.Complete();

	// Second wrong; calling dispose on it
	// MSDN-article is wrong; transaction is disposed twice in their code:
	// tuple.Item2.Dispose(); 
}