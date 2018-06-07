using DryIoc.Facilities.AutoTx.Abstraction;

namespace DryIoc.Facilities.AutoTx.Tests.TestClasses
{
    public class EmptyServiceWithInfo
    {
	    public EmptyServiceWithInfo(ServiceRequestInfo serviceRequestInfo)
	    {
		    ServiceRequestInfo = serviceRequestInfo;
	    }

	    public ServiceRequestInfo ServiceRequestInfo { get; }
	}
}
