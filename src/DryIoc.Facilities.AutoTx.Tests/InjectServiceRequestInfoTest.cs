using Castle.DynamicProxy;
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
		    _Container.Register<IServiceWithTransaction, SimpleServiceWithTransaction>(Reuse.Singleton);
		    _Container.Register<SimpleServiceWithTransaction>(Reuse.Singleton);
		    _Container.Register<EmptyServiceWithInfo>(Reuse.Singleton);
			_Container.Register<ServiceWithParentInfo>(Reuse.Transient);
		    _Container.AddFacility<AutoTxFacility>();
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
			Assert.NotNull(emptyService.ServiceWithParentInfo);
			Assert.NotNull(emptyService.ServiceWithParentInfo.ParentServiceRequestInfo);
			var parentServiceRequestInfo1 = emptyService.ServiceWithParentInfo.ParentServiceRequestInfo;
			Assert.AreEqual(typeof(EmptyServiceWithInfo), parentServiceRequestInfo1.ImplementationType);
			Assert.AreEqual(typeof(EmptyServiceWithInfo), parentServiceRequestInfo1.RequestInfo.ImplementationType);

			var simpleService = _Container.Resolve<SimpleServiceWithTransaction>();
			Assert.NotNull(simpleService);
			var parentServiceRequestInfo2 = simpleService.ServiceWithParentInfo.ParentServiceRequestInfo;
			Assert.AreEqual(typeof(SimpleServiceWithTransaction), parentServiceRequestInfo2.ImplementationType, "ImplementationType should base (without proxy)");
			Assert.IsTrue(ProxyUtil.IsProxyType(parentServiceRequestInfo2.RequestInfo.ImplementationType), "Request.ImplementationType should be proxy type");

			var simpleService2 = _Container.Resolve<IServiceWithTransaction>();
			Assert.NotNull(simpleService2);
		    var parentServiceRequestInfo3 = simpleService2.ServiceWithParentInfo.ParentServiceRequestInfo;
		    Assert.AreEqual(typeof(SimpleServiceWithTransaction), parentServiceRequestInfo3.ImplementationType, "ImplementationType should base (without proxy)");
		}
	}
}
