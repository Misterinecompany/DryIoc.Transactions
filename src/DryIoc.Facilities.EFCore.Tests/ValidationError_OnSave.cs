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
using System.Linq;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.EFCore.Tests.Framework;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using DryIoc.Facilities.EFCore.Tests.TestClasses.Entities;
using DryIoc.Transactions;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	internal class ValidationError_OnSave : EnsureSchema
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

		[Test]
		public void RunTest()
		{
			logger.Debug("starting test run");

			using (var x = container.ResolveScope<Test>())
			{
				x.Service.Run();
			}
		}
	}

	internal static class ContainerBuilder
	{
		public static Container Create()
		{
			var container = new Container();
			container.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			//container.UseInstance<INHibernateInstaller>(new ExampleInstaller(new ThrowingInterceptor())); //TODO remove this line
			
			container.Register<Test>(Reuse.Transient);
			container.Register<NestedTransactionService>(Reuse.Transient);

			container.AddAutoTx();
			container.AddEFCore();

			return container;
		}
	}

	public class Test
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly ExampleDbContextManager _dbContextManager;
		private Guid _thingId;

		public Test(ExampleDbContextManager dbContextManager)
		{
			_dbContextManager = dbContextManager;
		}

		
		public virtual void Run()
		{
			logger.Debug("run invoked");

			SaveNewThing();
			try
			{
				logger.Debug("chaning thing which will throw");

				ChangeThing();
			}
			catch (ApplicationException)
			{
				// this exception is expected - it is thrown by the ChangeThing method (simulates some validation)
			}

			// loading a new thing, in a new session!!
			var t = LoadThing();
			Assert.AreEqual(18.0, t.Value);
		}

		public virtual void RunWithRollback()
		{
			Exception error = null;
			try
			{
				SaveNewThingWithFollowingError();
			}
			catch (InvalidOperationException exception)
			{
				error = exception;
			}

			Assert.IsNotNull(error, "Saving should failed with InvalidOperationException");

			var thing = GetThing();
			Assert.IsNull(thing, "Saved item should rollback, but is still in database");
		}

		[Transaction]
		protected virtual void SaveNewThing()
		{
			var dbContext = _dbContextManager.OpenDbContext();

			var thing = new EfcThing
			{
				Value = 18.0
			};
			dbContext.Add(thing);
			dbContext.SaveChanges();

			_thingId = thing.Id;
		}

		[Transaction]
		protected virtual void SaveNewThingWithFollowingError()
		{
			using (var dbContext = _dbContextManager.OpenDbContext())
			{
				var thing = new EfcThing
				{
					Value = 37.0
				};
				dbContext.Add(thing);
				dbContext.SaveChanges();
				_thingId = thing.Id;
				
				throw new InvalidOperationException("Artificial error after saving item");
			}
		}

		[Transaction]
		protected virtual void ChangeThing()
		{
			var dbContext = _dbContextManager.OpenDbContext();
			var thing = dbContext.Find<EfcThing>(_thingId);
			thing.Value = 19.0;

			dbContext.SaveChanges();

			throw new ApplicationException("imaginary validation error");
		}

		//Removed Transaction attribute, Transaction is not needed to load a thing
		protected virtual EfcThing LoadThing()
		{
			var dbContext = _dbContextManager.OpenDbContext(); // we are expecting this to be a new session
			return dbContext.Find<EfcThing>(_thingId);
		}

		[Transaction]
		protected virtual EfcThing GetThing()
		{
			var dbContext = _dbContextManager.OpenDbContext();
			return dbContext.Things.SingleOrDefault(x => x.Id == _thingId);
		}
	}
}