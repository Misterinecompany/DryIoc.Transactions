using System;
using DryIoc.Facilities.AutoTx.Abstraction;
using Microsoft.Extensions.Logging;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class DryIocExtensions
    {
		public static void AddFacility<T>(this IContainer container) where T : IFacility, new()
		{
			var facility = new T();
			facility.Init(container);
	    }

	    public static void AddLoggerResolving(this IContainer container)
	    {
		    container.Register<ILogger>(Made.Of(
			    () => LoggerFactoryExtensions.CreateLogger(Arg.Of<ILoggerFactory>(), Arg.Index<Type>(0)),
			    request => request.Parent.ImplementationType));
		}

	    public static void Release(this IContainer container, object instance)
	    {
			// TODO probably not required
	    }
	}
}
