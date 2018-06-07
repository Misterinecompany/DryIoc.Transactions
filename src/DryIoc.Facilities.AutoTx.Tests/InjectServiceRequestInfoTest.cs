using Castle.Facilities.AutoTx;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.AutoTx.Tests
{
    public class InjectServiceRequestInfoTest
    {
	    private Container _Container;

	    [SetUp]
	    public void SetUp()
	    {
		    _Container = new Container();
		    _Container.AddFacility<AutoTxFacility>();
		    _Container.Register<SimpleServiceWithInfo>(Reuse.Singleton);
		    _Container.Register<EmptyServiceWithInfo>(Reuse.Singleton);
	    }

	    [TearDown]
	    public void TearDown()
	    {
		    _Container.Dispose();
	    }

	    [Test]
	    public void TestServiceRequestConstructorInjection()
	    {
		    var emptyService = _Container.Resolve<EmptyServiceWithInfo>();
			Assert.NotNull(emptyService);
			Assert.AreEqual(typeof(EmptyServiceWithInfo), emptyService.ServiceRequestInfo.ImplementationType);
			Assert.AreEqual(typeof(EmptyServiceWithInfo), emptyService.ServiceRequestInfo.RequestInfo.ImplementationType);

		    var simpleService = _Container.Resolve<SimpleServiceWithInfo>();
			Assert.NotNull(simpleService);
			Assert.AreEqual(typeof(SimpleServiceWithInfo), simpleService.ServiceRequestInfo.ImplementationType);
			Assert.AreNotEqual(typeof(SimpleServiceWithInfo), simpleService.ServiceRequestInfo.RequestInfo.ImplementationType);
	    }
	}
}
