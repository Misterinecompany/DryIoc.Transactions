using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Castle.Facilities.AutoTx;
using Castle.Transactions;
using Microsoft.Extensions.Logging;

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	public abstract class PerTransactionScopeContextBase : IScopeContext
	{
		protected readonly object LockObject = new object();

		private readonly Dictionary<string, Scope> _ScopePerTransactionIdStorage;
		private readonly ILogger _Logger;
		protected bool _Disposed;

		public PerTransactionScopeContextBase(ILogger logger)
		{
			_Logger = logger;
			_ScopePerTransactionIdStorage = new Dictionary<string, Scope>();
		}

		public abstract string RootScopeName { get; }

		protected abstract Maybe<ITransaction> GetSemanticTransaction();

		public bool IsCurrentTransaction => GetSemanticTransaction().HasValue;

		public IScope GetCurrentOrDefault()
		{
			if (_Disposed)
				throw new ObjectDisposedException("PerTransactionLifestyleManagerBase",
					"You cannot resolve with a disposed lifestyle.");

			if (!IsCurrentTransaction)
			{
				throw new MissingTransactionException(
					"No transaction in context when trying to instantiate model for resolve type. "
					+ "If you have verified that your call stack contains a method with the [Transaction] attribute, "
					+ "then also make sure that you have registered the AutoTx Facility."
				);
			}

			var transaction = GetSemanticTransaction().Value;

			Contract.Assume(transaction.State != TransactionState.Disposed,
				"because then it would not be active but would have been popped");

			var key = transaction.LocalIdentifier;

			if (_ScopePerTransactionIdStorage.TryGetValue(key, out var scope))
			{
				return scope;
			}

			lock (LockObject)
			{
				scope = new Scope(name: RootScopeName);
				_ScopePerTransactionIdStorage.Add(key, scope);

				transaction.Inner.TransactionCompleted += (sender, args) =>
				{
					lock (LockObject)
					{
						if (!_Disposed)
						{
							Contract.Assume(_ScopePerTransactionIdStorage.Count > 0);

							_ScopePerTransactionIdStorage.Remove(key);
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
					if (_ScopePerTransactionIdStorage.Count > 0)
					{
						var items = string.Join(
							$", {Environment.NewLine}",
							_ScopePerTransactionIdStorage
								.Select(x => $"(id: {x.Key}, item: {x.Value.ToString()})")
								.ToArray());

						if (_Logger.IsEnabled(LogLevel.Warning))
							_Logger.LogWarning("Storage contains {0} items! Items: {{ {1} }}",
								_ScopePerTransactionIdStorage.Count,
								items);
					}

					_ScopePerTransactionIdStorage.Clear();
				}
			}
			finally
			{
				_Disposed = true;
			}
		}
	}

	public class PerTransactionScopeContext : PerTransactionScopeContextBase
	{
		private readonly ITransactionManager _TransactionManager;
		public const string ScopeContextName = "TransactionScopeContext";

		public PerTransactionScopeContext(ITransactionManager transactionManager, ILogger logger) : base(logger)
		{
			_TransactionManager = transactionManager;
		}

		public override string RootScopeName => ScopeContextName;

		protected override Maybe<ITransaction> GetSemanticTransaction()
		{
			if (_Disposed)
				throw new ObjectDisposedException(nameof(PerTransactionScopeContext),
					"The lifestyle manager is disposed and cannot be used.");

			return _TransactionManager.CurrentTransaction;
		}
	}

	public class PerTopTransactionScopeContext : PerTransactionScopeContextBase
	{
		private readonly ITransactionManager _TransactionManager;
		public const string ScopeContextName = "TopTransactionScopeContext";

		public PerTopTransactionScopeContext(ITransactionManager transactionManager, ILogger logger) : base(logger)
		{
			_TransactionManager = transactionManager;
		}

		public override string RootScopeName => ScopeContextName;

		protected override Maybe<ITransaction> GetSemanticTransaction()
		{
			if (_Disposed)
				throw new ObjectDisposedException(nameof(PerTopTransactionScopeContext),
					"The lifestyle manager is disposed and cannot be used.");

			return _TransactionManager.CurrentTopTransaction;
		}
	}
}