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
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.NHibernate.Tests.Extensions;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using DryIoc.Transactions;
using NHibernate;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	public class AdvancedUseCase_DependentTransactionsAreNotFlushingToStore
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private Container c;

		[SetUp]
		public void SetUp()
		{
			c = GetContainer();
		}

		[Test]
		[Ignore("Haven't found a repro yet")]
		public void MainTx_ThenRW_ThenR_ThenDependentWrite()
		{
			// given
			var component = c.Resolve<ReproClass>();
			component.SaveNewThingSetup();

			// then
			component.MainInvocation();
			Assert.Fail("we haven't solved this test yet");
		}

		private static Container GetContainer()
		{
			var c = new Container();

			c.Register<INHibernateInstaller, ExampleInstaller>(Reuse.Singleton);

			c.AddNLogLogging();
			c.AddAutoTx();
			c.AddNHibernate(AmbientTransactionOption.Enabled);

			c.Register<ReproClass>(Reuse.Singleton);

			Assert.That(c.IsRegistered(typeof(ITransactionManager)));

			return c;
		}
	}

	public class ReproClass
	{
		private readonly Func<ISession> getSession;
		private readonly ITransactionManager manager;
		private Guid thingId;

		public ReproClass(Func<ISession> getSession, ITransactionManager manager)
		{
			if (getSession == null) throw new ArgumentNullException(nameof(getSession));
			this.getSession = getSession;
			this.manager = manager;
		}

		[Transaction]
		public virtual Guid SaveNewThingSetup()
		{
			return thingId = (Guid)getSession().Save(new Thing(19.0));
		}

		[Transaction]
		public virtual void MainInvocation()
		{
			var t = Read1();
			Write1(t);

			Read2();
			Write2InTx(t);
		}

		[Transaction]
		protected virtual void Write2InTx(Thing t)
		{
			Assert.That(manager.Count, Is.EqualTo(2));
			getSession().Delete(t);
		}

		private void Read2()
		{
			getSession().Load<Thing>(thingId);
		}

		private void Write1(Thing thing)
		{
			thing.Value = 20.0;
			getSession().Update(thing);
		}

		private Thing Read1()
		{
			return getSession().Load<Thing>(thingId);
		}
	}
}