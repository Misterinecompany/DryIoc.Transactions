using DryIoc.Facilities.AutoTx.Utils;
using DryIoc.Transactions;

namespace DryIoc.Facilities.AutoTx.Tests.TestClasses
{
    public class ServiceWithParentInfo
	{
		public ServiceWithParentInfo(ParentServiceRequestInfo parentServiceRequestInfo)
		{
			ParentServiceRequestInfo = parentServiceRequestInfo;
		}

		public ParentServiceRequestInfo ParentServiceRequestInfo { get; }
	}

	public class EmptyServiceWithInfo
	{
		public EmptyServiceWithInfo(ServiceWithParentInfo serviceWithParentInfo)
		{
			ServiceWithParentInfo = serviceWithParentInfo;
		}

		public ServiceWithParentInfo ServiceWithParentInfo { get; }
	}

	public class SimpleServiceWithTransaction : IServiceWithTransaction
	{
		public SimpleServiceWithTransaction(ServiceWithParentInfo serviceWithParentInfo1)
		{
			ServiceWithParentInfo = serviceWithParentInfo1;
		}

		public ServiceWithParentInfo ServiceWithParentInfo { get; }

		[Transaction]
		public virtual void TransactionMethod()
		{
		}

		[Transaction]
		public void TransactionMethodFromInterface()
		{
		}
	}

	public interface IServiceWithTransaction
	{
		ServiceWithParentInfo ServiceWithParentInfo { get; }

		void TransactionMethodFromInterface();
	}
}
