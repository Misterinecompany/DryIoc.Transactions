using System;
using System.Runtime.Serialization;

namespace DryIoc.Facilities.NHibernate.Errors
{
    public class NHibernateFacilityException : InvalidOperationException
    {
	    public NHibernateFacilityException()
	    {
	    }

	    protected NHibernateFacilityException(SerializationInfo info, StreamingContext context) : base(info, context)
	    {
	    }

	    public NHibernateFacilityException(string message) : base(message)
	    {
	    }

	    public NHibernateFacilityException(string message, Exception innerException) : base(message, innerException)
	    {
	    }
    }
}
