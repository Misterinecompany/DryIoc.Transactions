using DryIoc.Transactions;

namespace DryIoc.Facilities.AutoTx.Tests.TestClasses
{
	public class InheritedMyService:MyService
	{
		public InheritedMyService(ITransactionManager manager) : base(manager)
		{
		}
	}
}