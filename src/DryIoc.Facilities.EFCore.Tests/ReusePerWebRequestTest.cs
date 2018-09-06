// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using DryIoc.Facilities.AutoTx.Errors;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class ReusePerWebRequestTest
	{
		private IContainer container;

		[SetUp]
		public void SetUp()
		{
			//container = new Container().WithDependencyInjectionAdapter(); // the same configuration as for ASP.NET Core (test per web-request life style)
			container = new Container(); // use normal Container because rule .WithFactorySelector(Rules.SelectLastRegisteredFactory()) overrides registration configuration
			container.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			container.AddAutoTx();
			container.AddEFCore(DefaultLifeStyleOption.PerWebRequest);

			//var app = new HttpApplication();
			//var lifestyle = new PerWebRequestLifestyleModule();
			//lifestyle.Init(app);
		}

		[TearDown]
		public void TearDown()
		{
			container.Dispose();
		}

		[Test]
		public void RegisterAndResolve()
		{
			try
			{
				using (var scope = container.ResolveScope<ExampleDbContext>())
				{
					Console.WriteLine(scope.Service.DbContextId);
				}

				Assert.Fail("Not in web request, should not resolve.");
			}
			catch (InvalidOperationException e)
			{
				Assert.That(e.Message, Does.Contain("No current scope available"));
			}
		}

		[Test]
		public void resolving_per_tx()
		{
			Assert.Throws<MissingTransactionException>(() => container.Resolve<ExampleDbContext>(ExampleInstaller.Key + EFCoreFacility.DbContextPerTxSuffix));
		}

		[Test]
		public void resolving_transient()
		{
			var s1 = container.Resolve<ExampleDbContext>(ExampleInstaller.Key + EFCoreFacility.DbContextTransientSuffix);
			var s2 = container.Resolve<ExampleDbContext>(ExampleInstaller.Key + EFCoreFacility.DbContextTransientSuffix);
			Assert.That(s1.DbContextId, Is.Not.EqualTo(s2.DbContextId));
			container.Release(s1);
			container.Release(s2);
		}
	}
}