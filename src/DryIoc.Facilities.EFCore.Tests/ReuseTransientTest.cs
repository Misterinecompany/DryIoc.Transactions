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
using DryIoc.Facilities.AutoTx;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class ReuseTransientTest
	{
		private IContainer container;

		[SetUp]
		public void given_transient_registration()
		{
			container = new Container();
			container.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			container.AddAutoTx();
			container.AddEFCore(DefaultLifeStyleOption.Transient);
		}

		[TearDown]
		public void TearDown()
		{
			container.Dispose();
		}

		[Test]
		public void then_all_new_ids()
		{
			var s = container.Resolve<Func<ExampleDbContext>>();
			var s1 = s();
			var s2 = s();

			Assert.That(
				s1.DbContextId,
				Is.Not.EqualTo(s2.DbContextId));
		}

		[Test]
		public void then_when_resolving_per_tx_throws_outside()
		{
			Assert.Throws<MissingTransactionException>(
				() => container.Resolve<ExampleDbContext>(ExampleInstaller.Key + EFCoreFacility.DbContextPerTxSuffix));
		}

		[Test]
		public void then_per_web_throws()
		{
			Assert.Throws<ContainerException>(
				() => container.Resolve<ExampleDbContext>(ExampleInstaller.Key + EFCoreFacility.DbContextPWRSuffix));
		}
	}
}