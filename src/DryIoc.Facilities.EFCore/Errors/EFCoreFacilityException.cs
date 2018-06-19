using System;
using System.Runtime.Serialization;

namespace DryIoc.Facilities.EFCore.Errors
{
    public class EFCoreFacilityException : InvalidOperationException
	{
	    public EFCoreFacilityException()
	    {
	    }

	    protected EFCoreFacilityException(SerializationInfo info, StreamingContext context) : base(info, context)
	    {
	    }

	    public EFCoreFacilityException(string message) : base(message)
	    {
	    }

	    public EFCoreFacilityException(string message, Exception innerException) : base(message, innerException)
	    {
	    }
    }
}
