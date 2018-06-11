using System;
using System.Linq;
using Castle.DynamicProxy;
using ImTools;

namespace DryIoc.Facilities.AutoTx.Extensions
{
	public static class DryIocInterceptionExtensions
	{
		private static readonly Lazy<DefaultProxyBuilder> _ProxyBuilder = new Lazy<DefaultProxyBuilder>(() => new DefaultProxyBuilder());

		private static DefaultProxyBuilder ProxyBuilder => _ProxyBuilder.Value;

		public static void Intercept<TInterceptor>(this IRegistrator registrator, Type serviceType, object serviceKey = null)
			where TInterceptor : class, IInterceptor
		{
			registrator.Intercept<TInterceptor>(serviceType, serviceType, serviceKey);
		}

		public static void Intercept<TInterceptor>(this IRegistrator registrator, Type serviceType, Type proxyType, object serviceKey = null)
			where TInterceptor : class, IInterceptor
		{
			var decoratorSetup = serviceKey == null
				? Setup.DecoratorWith(useDecorateeReuse: true)
				: Setup.DecoratorWith(r => serviceKey.Equals(r.ServiceKey), useDecorateeReuse: true);

			registrator.Register(serviceType, proxyType,
				made: Made.Of(type => type.GetPublicInstanceConstructors().SingleOrDefault(c => c.GetParameters().Length != 0),
					Parameters.Of.Type<IInterceptor[]>(typeof(TInterceptor[]))),
				setup: decoratorSetup);
		}

		public static Type CreateProxy(this IRegistrator container, Type serviceType)
		{
			Type proxyType;
			if (serviceType.IsInterface())
				proxyType = ProxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(
					serviceType, ArrayTools.Empty<Type>(), ProxyGenerationOptions.Default);
			else if (serviceType.IsClass())
				proxyType = ProxyBuilder.CreateClassProxyType(
					serviceType, ArrayTools.Empty<Type>(), ProxyGenerationOptions.Default);
			else
				throw new ArgumentException(
					$"Intercepted service type {serviceType} is not a supported: it is nor class nor interface");

			return proxyType;
		}
	}
}
