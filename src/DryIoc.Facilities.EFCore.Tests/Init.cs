using DryIoc.Facilities.EFCore.Tests.Extensions;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class Init
	{
		[Test]
		public void TestMethod1()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton);
			
			//TODO add EFC facilty and try load data
		}
	}
}
