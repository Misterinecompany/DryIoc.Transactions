using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.EFCore.Tests.Framework;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	internal class RollbackTest : EnsureSchema
	{
		private Container _Container;

		[SetUp]
		public void SetUp()
		{
			_Container = ContainerBuilder.Create();
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
