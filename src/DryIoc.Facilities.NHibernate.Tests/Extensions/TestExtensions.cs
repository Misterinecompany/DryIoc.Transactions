using DryIoc.Facilities.AutoTx.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace DryIoc.Facilities.NHibernate.Tests.Extensions
{
    public static class TestExtensions
    {
	    public static void AddNLogLogging(this IContainer container)
	    {
			container.Register<ILoggerFactory, NLogLoggerFactory>(Reuse.Singleton);
		    if (!container.IsRegistered(typeof(ILogger)))
		    {
			    container.AddLoggerResolving();
		    }
	    }
    }
}
