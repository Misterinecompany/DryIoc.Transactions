using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.AutoTx.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.AutoTx.Tests
{
	public class InspectingExistingComponents_OnInit
	{
		[Test]
		public void Register_Then_AddFacility_ThenInvokeTransactionalMethod()
		{
			var container = new Container();
			container.Register<MyService>(Reuse.Transient);
			container.AddAutoTx();

			// this throws if we have not implemented this feature
			using (var s = container.ResolveScope<MyService>())
				s.Service.VerifyInAmbient();
		}
	}
}