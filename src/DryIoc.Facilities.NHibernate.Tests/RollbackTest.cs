using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.NHibernate.Tests.Framework;
using DryIoc.Transactions;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	[TestFixture(AmbientTransactionOption.Enabled)]
	[TestFixture(AmbientTransactionOption.Disabled)]
	internal class RollbackTest : EnsureSchema
	{
		private readonly AmbientTransactionOption _AmbientTransaction;
		private Container _Container;

		public RollbackTest(AmbientTransactionOption ambientTransaction)
		{
			_AmbientTransaction = ambientTransaction;
		}

		[SetUp]
		public void SetUp()
		{
			_Container = ContainerBuilder.Create(_AmbientTransaction);
		}

		[TearDown]
		public void TearDown()
		{
			_Container.Dispose();
		}

		[Test]
		public void RunTest()
		{
			using (var x = _Container.ResolveScope<Test>())
			{
				x.Service.RunWithRollback();
			}
		}
	}
}
