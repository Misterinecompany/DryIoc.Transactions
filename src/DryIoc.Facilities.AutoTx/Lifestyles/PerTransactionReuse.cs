using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.Facilities.AutoTx;
using Castle.Transactions;
using DryIoc.Facilities.AutoTx.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	public class PerTransactionReuse : IReuse, IReuseV3
	{
		private readonly PerTransactionScopeContext _PerTransactionScopeContext;

		public PerTransactionReuse(PerTransactionScopeContext perTransactionScopeContext) // TODO get scope context from container in Wrapper
		{
			_PerTransactionScopeContext = perTransactionScopeContext;
		}

		public int Lifespan => 50;

		#region IReuseV3 implementation

		/// <summary>Returns item from transaction scope.</summary>
		/// <param name="scopeContext">Transaction scope context to select from.</param>
		/// <param name="itemId">Scoped item ID for lookup.</param>
		/// <param name="createValue">Delegate for creating the item.</param>
		/// <returns>Reused item.</returns>
		public static object GetOrAddItemOrDefault(IScopeContext scopeContext, int itemId, CreateScopedValue createValue)
		{
			var scope = scopeContext.GetCurrentOrDefault();
			return scope == null ? null : scope.GetOrAdd(itemId, createValue);
		}

		private static readonly MethodInfo _GetOrAddOrDefaultMethod =
			typeof(PerTransactionReuse).GetSingleMethodOrNull("GetOrAddItemOrDefault");

		/// <summary>Returns expression call to <see cref="GetOrAddItemOrDefault"/>.</summary>
		public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
		{
			var itemId = trackTransientDisposable ? -1 : request.FactoryID;

			return Expression.Call(_GetOrAddOrDefaultMethod,
				Expression.Constant(_PerTransactionScopeContext),
				Expression.Constant(itemId),
				Expression.Lambda<CreateScopedValue>(createItemExpr));
		}

		public bool CanApply(Request request)
		{
			return _PerTransactionScopeContext.IsCurrentTransaction;
		}

		private readonly Lazy<Expression> _PerTransactionReuseExpr = new Lazy<Expression>(() =>
			Expression.Field(null, typeof(AutoTxReuse).GetFieldOrNull("PerTransaction")));

		public Expression ToExpression(Func<object, Expression> fallbackConverter)
		{
			return _PerTransactionReuseExpr.Value;
		}

		#endregion

		#region Reuse implementation

		public IScope GetScopeOrDefault(Request request)
		{
			return _PerTransactionScopeContext.GetCurrentOrDefault();
		}

		public Expression GetScopeExpression(Request request)
		{
			return Throw.For<Expression>(Error.Of("Obsolete"));
		}

		public int GetScopedItemIdOrSelf(int factoryId, Request request)
		{
			return _PerTransactionScopeContext.GetCurrentOrDefault().GetScopedItemIdOrSelf(factoryId);
		}

		#endregion
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

		public bool IsCurrentTransaction => _TransactionManager.CurrentTransaction.HasValue;

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
