using Castle.Transactions;
using DryIoc.Facilities.AutoTx.Abstraction;

namespace DryIoc.Facilities.AutoTx.Tests.TestClasses
{
	public class SimpleServiceWithInfo
	{
		public SimpleServiceWithInfo(ServiceRequestInfo serviceRequestInfo)
		{
			ServiceRequestInfo = serviceRequestInfo;
		}

		public ServiceRequestInfo ServiceRequestInfo { get; }

		[Transaction]
		public virtual void TransactionMethod()
		{
		}
	}
}