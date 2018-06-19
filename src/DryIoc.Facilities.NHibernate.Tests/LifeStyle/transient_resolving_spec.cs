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
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using NHibernate;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests.LifeStyle
{
	public class transient_resolving_spec
	{
		private IContainer container;

		[SetUp]
		public void given_transient_registration()
		{
			container = new Container();
			container.Register<INHibernateInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			container.AddAutoTx();
			container.AddNHibernate(DefaultSessionLifeStyleOption.SessionTransient);
		}

		[TearDown]
		public void TearDown()
		{
			container.Dispose();
		}

		[Test]
		public void then_all_new_ids()
		{
			var s = container.Resolve<Func<ISession>>();
			var s1 = s();
			var s2 = s();

			Assert.That(
				s1.GetSessionImplementation().SessionId,
				Is.Not.EqualTo(s2.GetSessionImplementation().SessionId));
		}

		[Test]
		public void then_when_resolving_per_tx_throws_outside()
		{
			Assert.Throws<MissingTransactionException>(
				() => container.Resolve<ISession>(ExampleInstaller.Key + NHibernateFacility.SessionPerTxSuffix));
		}

		[Test]
		public void then_per_web_throws()
		{
			Assert.Throws<ContainerException>(
				() => container.Resolve<ISession>(ExampleInstaller.Key + NHibernateFacility.SessionPWRSuffix));
		}
	}
}