using System;
using DryIoc.Transactions;
using Microsoft.Extensions.Logging;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class DryIocExtensions
    {
		public static void AddAutoTx(this IContainer container, AmbientTransactionOption ambientTransaction = AmbientTransactionOption.Enabled)
		{
			var autoTxFacility = new AutoTxFacility();
			autoTxFacility.Init(container, ambientTransaction);
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
