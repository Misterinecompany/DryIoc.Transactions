﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using DryIoc.Facilities.AutoTx.Extensions;

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	public abstract class PerTransactionReuseBase : IReuse, IReuseV3
	{
		private readonly PerTransactionScopeContextBase _PerTransactionScopeContextBase;

		public PerTransactionReuseBase(PerTransactionScopeContextBase perTransactionScopeContextBase)
		{
			_PerTransactionScopeContextBase = perTransactionScopeContextBase;
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
			typeof(PerTransactionReuseBase).GetSingleMethodOrNull("GetOrAddItemOrDefault");

		/// <summary>Returns expression call to <see cref="GetOrAddItemOrDefault"/>.</summary>
		public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
		{
			var itemId = trackTransientDisposable ? -1 : request.FactoryID;

			return Expression.Call(_GetOrAddOrDefaultMethod,
				Expression.Constant(_PerTransactionScopeContextBase),
				Expression.Constant(itemId),
				Expression.Lambda<CreateScopedValue>(createItemExpr));
		}

		public bool CanApply(Request request)
		{
			return _PerTransactionScopeContextBase.IsCurrentTransaction;
		}

		public abstract Expression ToExpression(Func<object, Expression> fallbackConverter);

		#endregion

		#region Reuse implementation

		public IScope GetScopeOrDefault(Request request)
		{
			return _PerTransactionScopeContextBase.GetCurrentOrDefault();
		}

		public Expression GetScopeExpression(Request request)
		{
			return Throw.For<Expression>(Error.Of("Obsolete"));
		}

		public int GetScopedItemIdOrSelf(int factoryId, Request request)
		{
			return _PerTransactionScopeContextBase.GetCurrentOrDefault().GetScopedItemIdOrSelf(factoryId);
		}

		#endregion
	}

	public class PerTransactionReuse : PerTransactionReuseBase
	{
		public PerTransactionReuse(PerTransactionScopeContext perTransactionScopeContext) : base(perTransactionScopeContext)
		{
		}

		public static readonly Lazy<Expression> PerTransactionReuseExpr = new Lazy<Expression>(() =>
			Expression.Field(null, typeof(AutoTxReuse).GetFieldOrNull("PerTransaction")));

		public override Expression ToExpression(Func<object, Expression> fallbackConverter)
		{
			return PerTransactionReuseExpr.Value;
		}
	}

	public class PerTopTransactionReuse : PerTransactionReuseBase
	{
		public PerTopTransactionReuse(PerTopTransactionScopeContext perTopTransactionScopeContext) : base(perTopTransactionScopeContext)
		{
		}

		public static readonly Lazy<Expression> PerTopTransactionReuseExpr = new Lazy<Expression>(() =>
			Expression.Field(null, typeof(AutoTxReuse).GetFieldOrNull("PerTopTransaction")));

		public override Expression ToExpression(Func<object, Expression> fallbackConverter)
		{
			return PerTopTransactionReuseExpr.Value;
		}
	}
}
