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
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.NHibernate.Tests.Framework;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using DryIoc.Transactions;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	internal class NestedTransactions : EnsureSchema
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private Container container;

		[SetUp]
		public void SetUp()
		{
			container = ContainerBuilder.Create();
		}

		[TearDown]
		public void TearDown()
		{
			container.Dispose();
		}

		/// <summary>
		/// This test shows that several transaction methods can be attached to the same transaction
		/// </summary>
		[Test]
		public void NestedTransactionFeature()
		{
			logger.Debug("starting test run");

			using (var x = container.ResolveScope<NestedTransactionService>())
			{
				x.Service.Run();
			}
		}

		/// <summary>
		/// This test shows that an objects created in a call chain is not persisted
		/// </summary>
		[Test]
		public void Transaction()
		{
			//Arrange
			logger.Debug("starting test run");
			var x = container.ResolveScope<NestedTransactionService>();
			int thingCount = x.Service.GetThingsCount();

			//Act
			x.Service.RunAndAssert();

			//Assert
			Assert.AreEqual(thingCount + 1, x.Service.GetThingsCount(), "A thing was created and transaction commited");
		}

		/// <summary>
		/// This test shows that objects are not persisted if something fails
		/// </summary>
		[Test]
		public void Rollback()
		{
			//Arrange
			logger.Debug("starting test run");
			var x = container.ResolveScope<NestedTransactionService>();
			int thingCount = x.Service.GetThingsCount();

			//Act
			try{
				x.Service.RunAndFail();
			}
			catch{
			}

			//Assert
			Assert.AreEqual(thingCount, x.Service.GetThingsCount(), "No thing was created");
		}
	}

	public class NestedTransactionService
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly ISessionManager sessionManager;
		private Guid thingId;

		public NestedTransactionService(ISessionManager sessionManager)
		{
			this.sessionManager = sessionManager;
		}

		[Transaction]
		public virtual void Run()
		{
			
			SaveNewThing();
			SaveNewThing();
		}

		[Transaction]
		public virtual void RunAndAssert()
		{
			//Arrange
			int thingCount = GetThingsCount();

			//Act
			SaveNewThing();

			//Assert
			Assert.AreEqual(thingCount + 1, GetThingsCount(), "New thing was created, but transaction is not commited yet");
		}

		[Transaction]
		public virtual void RunAndFail()
		{
			SaveNewThing();

			//Force a Fail so I can test the nested rollback
			throw new Exception();

			
		}
		

		[Transaction]
		protected virtual void SaveNewThing()
		{
			var s = sessionManager.OpenSession();
			var thing = new Thing(18.0);
			thingId = (Guid)s.Save(thing);
		}

		public virtual int GetThingsCount()
		{
			var s = sessionManager.OpenSession();
			return s.QueryOver<Thing>().RowCount();
		}
	}
}