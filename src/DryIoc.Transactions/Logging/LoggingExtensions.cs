using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace DryIoc.Transactions.Logging
{
    public static class LoggingExtensions
    {
		public static ILogger CreateChildLogger(this ILoggerFactory loggerFactory, string name, Type parentType)
	    {
		    return loggerFactory.CreateLogger($"{TypeNameHelper.GetTypeDisplayName(parentType)}.{name}");
	    }
	}
}
