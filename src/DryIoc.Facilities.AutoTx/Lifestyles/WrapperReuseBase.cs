using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Facilities.AutoTx.Lifestyles
{
	/// <summary>
	/// The lifestyle class with underlying lifestyle class which is resolved from the Container
	/// </summary>
	/// <typeparam name="T">Primary lifestyle manager which has its constructor resolved through the Container.</typeparam>
	public abstract class WrapperReuseBase<T> : IReuse, IReuseV3 where T : PerTransactionReuseBase
	{
		private object _Lock = new object();
		private ILogger _Logger;
		private IContainer _LifestyleKernel;
		protected T _Lifestyle1;
		private bool _Disposed;
		private bool _Initialized;

		public int Lifespan => 50;

		public Expression Apply(Request request, bool trackTransientDisposable, Expression createItemExpr)
		{
			return GetInnerReuse(request).Apply(request, trackTransientDisposable, createItemExpr);
		}

		public bool CanApply(Request request)
		{
			return GetInnerReuse(request).CanApply(request);
		}

		public abstract Expression ToExpression(Func<object, Expression> fallbackConverter);

		public IScope GetScopeOrDefault(Request request)
		{
			return GetInnerReuse(request).GetScopeOrDefault(request);
		}

		public Expression GetScopeExpression(Request request)
		{
			return GetInnerReuse(request).GetScopeExpression(request);
		}

		public int GetScopedItemIdOrSelf(int factoryId, Request request)
		{
			return GetInnerReuse(request).GetScopedItemIdOrSelf(factoryId, request);
		}

		private T GetInnerReuse(Request request)
		{
			if (!_Initialized)
			{
				lock (_Lock)
				{
					if (!_Initialized)
					{
						_Lifestyle1 = Init(request);
					}
				}
			}

			return _Lifestyle1;
		}

		public T Init(Request request)
		{
			Contract.Ensures(_Lifestyle1 != null);
			Contract.Ensures(_Initialized);

			var kernel = request.Container;

			// check ILoggerFactory is registered
			if (kernel.IsRegistered(typeof(ILoggerFactory)))
			{
				// get logger factory instance
				var loggerFactory = kernel.Resolve<ILoggerFactory>();
				// create logger
				_Logger = loggerFactory.CreateLogger(typeof(WrapperReuseBase<T>));
			}
			else
			{
				_Logger = NullLogger.Instance;
			}

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("initializing (for component: {0})", request.ServiceType);

			//_LifestyleKernel.Register<T>(Reuse.Transient, serviceKey: "T.lifestyle");
			//kernel.AddChildKernel(_LifestyleKernel);

			//try
			//{
			//	_Lifestyle1 = _LifestyleKernel.Resolve<T>();
			//}
			//finally
			//{
			//	kernel.RemoveChildKernel(_LifestyleKernel);
			//}

			_Lifestyle1 = kernel.New<T>();

			Contract.Assume(_Lifestyle1 != null,
				"lifestyle1 can't be null because the Resolve<T> call will throw an exception if a matching service wasn't found");

			_Logger.LogDebug("initialized");

			_Initialized = true;

			return _Lifestyle1;
		}
	}

	public class WrapperPerTransactionReuse : WrapperReuseBase<PerTransactionReuse>
	{
		public override Expression ToExpression(Func<object, Expression> fallbackConverter)
		{
			return PerTransactionReuse.PerTransactionReuseExpr.Value;
		}
	}

	public class WrapperPerTopTransactionReuse : WrapperReuseBase<PerTopTransactionReuse>
	{
		public override Expression ToExpression(Func<object, Expression> fallbackConverter)
		{
			return PerTopTransactionReuse.PerTopTransactionReuseExpr.Value;
		}
	}
}
