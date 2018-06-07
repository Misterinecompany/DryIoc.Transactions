using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using Castle.Facilities.AutoTx;
using Castle.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	public class PerTransactionReuse : IReuse
	{
		public IScope GetScopeOrDefault(Request request)
		{
			throw new System.NotImplementedException();
		}

		public Expression GetScopeExpression(Request request)
		{
			throw new System.NotImplementedException();
		}

		public int GetScopedItemIdOrSelf(int factoryID, Request request)
		{
			throw new System.NotImplementedException();
		}

		public int Lifespan { get; }
	}

	public class PerTransactionScopeContext : IScopeContext
	{
		public const string ScopeContextName = "TransactionScopeContext";

		protected readonly object LockObject = new object();

		private readonly Dictionary<string, Scope> _Storage;
		private readonly ITransactionManager _TransactionManager;
		private ILogger _Logger = NullLogger.Instance;
		protected bool _Disposed;

		public PerTransactionScopeContext(ITransactionManager transactionManager)
		{
			_TransactionManager = transactionManager;
			_Storage = new Dictionary<string, Scope>();
		}

		public string RootScopeName => ScopeContextName;

		public IScope GetCurrentOrDefault()
		{
			if (_Disposed)
				throw new ObjectDisposedException("PerTransactionLifestyleManagerBase",
					"You cannot resolve with a disposed lifestyle.");

			if (!_TransactionManager.CurrentTransaction.HasValue)
			{
				throw new MissingTransactionException(
					"No transaction in context when trying to instantiate model for resolve type. "
					+ "If you have verified that your call stack contains a method with the [Transaction] attribute, "
					+ "then also make sure that you have registered the AutoTx Facility."
				);
			}

			var transaction = _TransactionManager.CurrentTransaction.Value;

			Contract.Assume(transaction.State != TransactionState.Disposed,
				"because then it would not be active but would have been popped");

			var key = transaction.LocalIdentifier;

			if (_Storage.TryGetValue(key, out var scope))
			{
				return scope;
			}

			lock (LockObject)
			{
				scope = new Scope(name: ScopeContextName);
				_Storage.Add(key, scope);

				transaction.Inner.TransactionCompleted += (sender, args) =>
				{
					lock (LockObject)
					{
						if (!_Disposed)
						{
							Contract.Assume(_Storage.Count > 0);

							_Storage.Remove(key);
						}
					}
				};
			}

			return scope;
		}

		public IScope SetCurrent(SetCurrentScopeHandler setCurrentScope)
		{
			throw new NotSupportedException("Setting the new scope is not supported, because the transaction scope is created automatically.");
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool managed)
		{
			Contract.Ensures(!managed || _Disposed);

			if (!managed)
				return;

			if (_Disposed)
			{
				if (_Logger.IsEnabled(LogLevel.Information))
				{
					_Logger.LogInformation(
						"repeated call to Dispose. will show stack-trace if logging is in debug mode as the next log line. this method call is idempotent");

					if (_Logger.IsEnabled(LogLevel.Debug))
						_Logger.LogDebug(new StackTrace().ToString());
				}

				return;
			}

			try
			{
				lock (LockObject)
				{
					if (_Storage.Count > 0)
					{
						var items = string.Join(
							$", {Environment.NewLine}",
							_Storage
								.Select(x => $"(id: {x.Key}, item: {x.Value.ToString()})")
								.ToArray());

						if (_Logger.IsEnabled(LogLevel.Warning))
							_Logger.LogWarning("Storage contains {0} items! Items: {{ {1} }}",
								_Storage.Count,
								items);
					}

					_Storage.Clear();
				}
			}
			finally
			{
				_Disposed = true;
			}
		}
	}
}
