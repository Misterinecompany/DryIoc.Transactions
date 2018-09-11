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

using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.NHibernate.Tests.Extensions;
using DryIoc.Facilities.NHibernate.Tests.Framework;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using DryIoc.Transactions;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	[TestFixture(AmbientTransactionOption.Enabled)]
	[TestFixture(AmbientTransactionOption.Disabled)]
	public class SimpleUseCase_SingleSave : EnsureSchema
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();
		private readonly AmbientTransactionOption _AmbientTransaction;
		private Container _Container;

		public SimpleUseCase_SingleSave(AmbientTransactionOption ambientTransaction)
		{
			_AmbientTransaction = ambientTransaction;
		}

		[SetUp]
		public void SetUp()
		{
			_Container = GetContainer(_AmbientTransaction);
		}

		[TearDown]
		public void TearDown()
		{
			_Logger.Debug("running tear-down, removing components");

			using (var s = _Container.ResolveScope<TearDownService>())
			{
				s.Service.ClearThings();
			}

			_Container.Dispose();
		}

		[Test]
		public void Smoke()
		{
		}

		[Test]
		public void SavingWith_PerTransaction_Lifestyle()
		{
			// given
			using (var scope = _Container.ResolveScope<ServiceUsingPerTransactionSessionLifestyle>())
			{
				// then
				scope.Service.AmbientTransactionOption = _AmbientTransaction;
				scope.Service.SaveNewThing();
				Assert.That(scope.Service.LoadNewThing(), Is.Not.Null, "because it was saved by the previous method call");
			}
		}

		private static Container GetContainer(AmbientTransactionOption ambientTransaction)
		{
			var c = new Container();

			c.Register<INHibernateInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			c.Register<ServiceUsingPerTransactionSessionLifestyle>(Reuse.Transient);
			c.Register<TearDownService>(Reuse.Transient);

			c.AddNLogLogging();
			c.AddAutoTx();
			c.AddNHibernate(ambientTransaction);

			Assert.That(c.IsRegistered(typeof(ITransactionManager)));

			return c;
		}
	}
}