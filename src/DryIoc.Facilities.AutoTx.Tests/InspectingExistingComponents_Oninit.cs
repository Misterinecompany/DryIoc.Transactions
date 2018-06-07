using Castle.Facilities.AutoTx.Testing;
using Castle.Facilities.AutoTx.Tests.TestClasses;
using DryIoc;
using DryIoc.Facilities.AutoTx.Extensions;
using NUnit.Framework;

namespace Castle.Facilities.AutoTx.Tests
{
	public class InspectingExistingComponents_OnInit
	{
		[Test]
		public void Register_Then_AddFacility_ThenInvokeTransactionalMethod()
		{
			var container = new Container();
			container.Register<MyService>(Reuse.Transient);
			container.AddFacility<AutoTxFacility>();

			// this throws if we have not implemented this feature
			using (var s = container.ResolveScope<MyService>())
				s.Service.VerifyInAmbient();
		}
	}
}