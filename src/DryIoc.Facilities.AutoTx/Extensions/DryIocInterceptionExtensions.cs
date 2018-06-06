using System;
using System.Linq;
using Castle.DynamicProxy;
using ImTools;

namespace DryIoc.Facilities.AutoTx.Extensions
{
	public static class DryIocInterceptionExtensions
	{
		private static readonly Lazy<DefaultProxyBuilder> _proxyBuilder = new Lazy<DefaultProxyBuilder>(() => new DefaultProxyBuilder());

		private static DefaultProxyBuilder ProxyBuilder => _proxyBuilder.Value;

		public static void Intercept<TService, TInterceptor>(this IRegistrator registrator, object serviceKey = null)
			where TInterceptor : class, IInterceptor
		{
			var serviceType = typeof(TService);

			registrator.Intercept<TInterceptor>(serviceType, serviceKey);
		}

		public static void Intercept<TInterceptor>(this IRegistrator registrator, Type serviceType, object serviceKey = null)
			where TInterceptor : class, IInterceptor
		{
			registrator.Intercept<TInterceptor>(serviceType, serviceType, serviceKey);
		}

		public static void Intercept<TInterceptor>(this IRegistrator registrator, Type serviceType, Type serviceImpl, object serviceKey = null)
			where TInterceptor : class, IInterceptor
		{
			Type proxyType;
			if (serviceImpl.IsInterface())
				proxyType = ProxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(
					serviceImpl, ArrayTools.Empty<Type>(), ProxyGenerationOptions.Default);
			else if (serviceImpl.IsClass())
				proxyType = ProxyBuilder.CreateClassProxyType(
					serviceImpl, ArrayTools.Empty<Type>(), ProxyGenerationOptions.Default);
			else
				throw new ArgumentException(string.Format(
					"Intercepted service type {0} is not a supported: it is nor class nor interface", serviceImpl));

			var decoratorSetup = serviceKey == null
				? Setup.DecoratorWith(useDecorateeReuse: true)
				: Setup.DecoratorWith(r => serviceKey.Equals(r.ServiceKey), useDecorateeReuse: true);

			registrator.Register(serviceType, proxyType,
				made: Made.Of(type => type.GetPublicInstanceConstructors().SingleOrDefault(c => c.GetParameters().Length != 0),
					Parameters.Of.Type<IInterceptor[]>(typeof(TInterceptor[]))),
				setup: decoratorSetup);
		}
	}
}
