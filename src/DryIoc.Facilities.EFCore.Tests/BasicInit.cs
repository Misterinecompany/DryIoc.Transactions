using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.EFCore.Tests.Extensions;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using DryIoc.Facilities.EFCore.Tests.TestClasses.Entities;
using DryIoc.Transactions;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class BasicInit
	{
		private Container _Container;

		[SetUp]
		public void SetUp()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			c.Register<SimpleService>(Reuse.Singleton);
			c.AddAutoTx();
			c.AddEFCore();
			_Container = c;
		}

		[Test]
		public void TrySaveAndLoadDataUsingDbContextManager()
		{
			var service = _Container.Resolve<SimpleService>();
			service.SaveSomeData();
		}

		[TearDown]
		public void TearDown()
		{
			_Container.Dispose();
		}
	}

	public class SimpleService
	{
		private readonly ExampleDbContextManager m_dbContextManager;

		public SimpleService(ExampleDbContextManager dbContextManager)
		{
			m_dbContextManager = dbContextManager;
		}

		[Transaction]
		public virtual void SaveSomeData()
		{
			var dbContext = m_dbContextManager.OpenDbContext();
			var newThing = new EfcThing
			{
				Value = 64.16
			};
			dbContext.Things.Add(newThing);
		}
	}
}