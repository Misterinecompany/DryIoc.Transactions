using System;
using System.Reflection;
using DryIoc.Facilities.AutoTx.Extensions;
#if NET461
using Expr = FastExpressionCompiler.ExpressionInfo;
#else
using Expr = System.Linq.Expressions.Expression;
#endif

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	public abstract class PerTransactionReuseBase : IReuse
	{
		private readonly PerTransactionScopeContextBase _PerTransactionScopeContextBase;

		public PerTransactionReuseBase(PerTransactionScopeContextBase perTransactionScopeContextBase)
		{
			_PerTransactionScopeContextBase = perTransactionScopeContextBase;
		}

		public int Lifespan => 50;

		public object Name => null;

		/// <summary>Returns item from transaction scope.</summary>
		/// <param name="scopeContext">Transaction scope context to select from.</param>
		/// <param name="request">Container Request info for resolving service</param>
		/// <param name="itemId">Scoped item ID for lookup.</param>
		/// <param name="createValue">Delegate for creating the item.</param>
		/// <returns>Reused item.</returns>
		public static object GetOrAddItemOrDefault(PerTransactionScopeContextBase scopeContext, Request request, int itemId, CreateScopedValue createValue)
		{
			var scope = scopeContext.GetCurrentOrDefault(request.ServiceType);
			return scope == null ? null : scope.GetOrAdd(itemId, createValue);
		}

		private static readonly MethodInfo _GetOrAddOrDefaultMethod =
			typeof(PerTransactionReuseBase).GetSingleMethodOrNull("GetOrAddItemOrDefault");

		/// <summary>Returns expression call to <see cref="GetOrAddItemOrDefault"/>.</summary>
		public Expr Apply(Request request, Expr serviceFactoryExpr)
		{
			var itemId = request.TracksTransientDisposable ? -1 : request.FactoryID;

			return Expr.Call(_GetOrAddOrDefaultMethod,
				Expr.Constant(_PerTransactionScopeContextBase),
				Expr.Constant(request),
				Expr.Constant(itemId),
				Expr.Lambda<CreateScopedValue>(serviceFactoryExpr));
		}

		public bool CanApply(Request request)
		{
			return _PerTransactionScopeContextBase.IsCurrentTransaction;
		}

		public abstract Expr ToExpression(Func<object, Expr> fallbackConverter);
	}

	public class PerTransactionReuse : PerTransactionReuseBase
	{
		public PerTransactionReuse(PerTransactionScopeContext perTransactionScopeContext) : base(perTransactionScopeContext)
		{
		}

		public static readonly Lazy<Expr> PerTransactionReuseExpr = new Lazy<Expr>(() =>
			Expr.Property(null, typeof(AutoTxReuse).GetPropertyOrNull("PerTransaction")));

		public override Expr ToExpression(Func<object, Expr> fallbackConverter)
		{
			return PerTransactionReuseExpr.Value;
		}
	}

	public class PerTopTransactionReuse : PerTransactionReuseBase
	{
		public PerTopTransactionReuse(PerTopTransactionScopeContext perTopTransactionScopeContext) : base(perTopTransactionScopeContext)
		{
		}

		public static readonly Lazy<Expr> PerTopTransactionReuseExpr = new Lazy<Expr>(() =>
			Expr.Property(null, typeof(AutoTxReuse).GetPropertyOrNull("PerTopTransaction")));

		public override Expr ToExpression(Func<object, Expr> fallbackConverter)
		{
			return PerTopTransactionReuseExpr.Value;
		}
	}
}
