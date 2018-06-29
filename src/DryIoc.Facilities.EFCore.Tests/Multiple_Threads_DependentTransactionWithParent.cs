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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.EFCore.Tests.Extensions;
using DryIoc.Facilities.EFCore.Tests.Framework;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using DryIoc.Facilities.EFCore.Tests.TestClasses.Entities;
using DryIoc.Transactions;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class Multiple_Threads_DependentTransactionWithParent : EnsureSchema
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Container container;

		[SetUp]
		public void SetUp()
		{
			container = new Container();
			container.AddNLogLogging();
			container.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			container.Register<ThreadedService>(Reuse.Transient);
			container.AddAutoTx();
			container.AddEFCore();
		}

		[TearDown]
		public void TearDown()
		{
			container.Dispose();
		}

		[Test]
		public void SameSessionInSameTransaction()
		{
			using (var threaded = new ResolveScope<ThreadedService>(container))
			{
				threaded.Service.VerifySameSession();
			}
		}

		[Test]
		public void SameSession_WithRecursion()
		{
			using (var threaded = new ResolveScope<ThreadedService>(container))
			{
				threaded.Service.VerifyRecursingSession();
			}
		}

		[Test]
		[Explicit]
		public void Forking_NewTransaction_Means_AnotherISessionReference()
		{
			using (var threaded = new ResolveScope<ThreadedService>(container))
			{
				threaded.Service.MainThreadedEntry();
				Assert.That(threaded.Service.CalculationsIds.Count, Is.EqualTo(Environment.ProcessorCount));
			}
		}

		[Test]
		public void Forking_InDependentTransaction_Means_PerTransactionLifeStyle_SoSameInstances()
		{
			using (var threaded = new ResolveScope<ThreadedService>(container))
				threaded.Service.VerifySameSessionInFork();
		}
	}

	public class ThreadedService
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly List<Guid> _calculationsIds = new List<Guid>();

		private readonly Func<ExampleDbContext> _getDbContext;
		private readonly ITransactionManager _manager;
		private Guid _mainThingId;

		public ThreadedService(Func<ExampleDbContext> getDbContext, ITransactionManager manager)
		{
			Contract.Requires(manager != null);
			Contract.Requires(getDbContext != null);

			_getDbContext = getDbContext;
			_manager = manager;
		}

		public List<Guid> CalculationsIds => _calculationsIds;

		#region Same instance tests

		[Transaction]
		public virtual void VerifySameSession()
		{
			var dbContext = _getDbContext();
			var id1 = dbContext.DbContextId;

			var dbContext2 = _getDbContext();
			Assert.That(dbContext2.DbContextId, Is.EqualTo(id1));
		}

		#endregion

		#region Recursion/multiple txs on call context

		[Transaction]
		public virtual void VerifyRecursingSession()
		{
			var myId = _getDbContext().DbContextId;
			CheckRecursingSession_ShouldBeSame(myId);
			CheckRecursingSessionWithoutTransaction_ShouldBeSame(myId);
			CheckForkedRecursingSession_ShouldBeDifferent(myId);
		}

		[Transaction]
		protected virtual void CheckRecursingSession_ShouldBeSame(Guid myId)
		{
			var dbContext = _getDbContext();
			Assert.That(myId, Is.EqualTo(dbContext.DbContextId));
		}

		[Transaction(Fork = true)]
		protected virtual void CheckForkedRecursingSession_ShouldBeDifferent(Guid myId)
		{
			var dbContext = _getDbContext();
			Assert.That(myId, Is.Not.EqualTo(dbContext.DbContextId));
		}

		protected virtual void CheckRecursingSessionWithoutTransaction_ShouldBeSame(Guid myId)
		{
			var dbContext = _getDbContext();
			Assert.That(myId, Is.EqualTo(dbContext.DbContextId));
		}

		#endregion

		#region Forking - Succeeding transactions

		[Transaction]
		public virtual void MainThreadedEntry()
		{
			var dbContext = _getDbContext();

			var newThing = new EfcThing
			{
				Value = 17.0
			};
			dbContext.Add(newThing);
			dbContext.SaveChanges();
			_mainThingId = newThing.Id;

			_logger.Debug("put some cores ({0}) to work!", Environment.ProcessorCount);

			for (var i = 0; i < Environment.ProcessorCount; i++)
			{
				CalculatePi(dbContext.DbContextId);
			}
		}

		[Transaction(Fork = true)]
		protected virtual void CalculatePi(Guid firstSessionId)
		{
			var dbContext = _getDbContext();

			Assert.That(dbContext.DbContextId, Is.Not.EqualTo(firstSessionId),
						"because ISession is not thread safe and we want per-transaction semantics when Fork=true");

			lock (_calculationsIds)
			{
				var newThing = new EfcThing
				{
					Value = 2 * CalculatePiInner(1)
				};
				dbContext.Add(newThing);
				dbContext.SaveChanges();
				_calculationsIds.Add(newThing.Id);
			}
		}

		protected double CalculatePiInner(int i)
		{
			if (i == 5000)
				return 1;

			return 1 + i / (2.0 * i + 1) * CalculatePiInner(i + 1);
		}

		#endregion

		#region Forking: (PerTransaction = same sessions per tx)

		[Transaction]
		public virtual void VerifySameSessionInFork()
		{
			_logger.Info("asserting for main thread");

			AssertSameDbContextId();

			_logger.Info("forking");
			VerifySameSessionInForkInner();
		}

		[Transaction(Fork = true)]
		protected virtual void VerifySameSessionInForkInner()
		{
			_logger.Info("asserting for task-thread");
			AssertSameDbContextId();
		}

		private void AssertSameDbContextId()
		{
			var dbContextId1 = _getDbContext().DbContextId;
			var dbContextId2 = _getDbContext().DbContextId;

			if (!dbContextId1.Equals(dbContextId2))
				_logger.Error("s1 != s2 in forked method");

			Assert.That(dbContextId1, Is.EqualTo(dbContextId2));
		}

		#endregion
	}
}