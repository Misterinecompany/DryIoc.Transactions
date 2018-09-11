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
using System.Collections;
using System.Transactions;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.NHibernate.Tests.Framework;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using DryIoc.Transactions;
using NHibernate;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NLog;
using NUnit.Framework;
using TransactionException = System.Transactions.TransactionException;

namespace DryIoc.Facilities.NHibernate.Tests
{
	using ITransaction = global::NHibernate.ITransaction;

	[TestFixture(AmbientTransactionOption.Enabled)]
	[TestFixture(AmbientTransactionOption.Disabled)]
	internal class ValidationError_OnSave : EnsureSchema
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();
		private readonly AmbientTransactionOption _AmbientTransaction;
		private Container _Container;

		public ValidationError_OnSave(AmbientTransactionOption ambientTransaction)
		{
			_AmbientTransaction = ambientTransaction;
		}

		[SetUp]
		public void SetUp()
		{
			_Container = ContainerBuilder.Create(_AmbientTransaction);
		}

		[TearDown]
		public void TearDown()
		{
			_Container.Dispose();
		}

		[Test]
		public void RunTest()
		{
			_Logger.Debug("starting test run");

			using (var x = _Container.ResolveScope<Test>())
			{
				x.Service.Run();
			}
		}
	}

	internal static class ContainerBuilder
	{
		public static Container Create(AmbientTransactionOption ambientTransaction)
		{
			var container = new Container();
			container.UseInstance<INHibernateInstaller>(new ExampleInstaller(new ThrowingInterceptor()));
			
			container.Register<Test>(Reuse.Transient);
			container.Register<NestedTransactionService>(Reuse.Transient);

			container.AddAutoTx();
			container.AddNHibernate(ambientTransaction);

			return container;
		}
	}

	internal class ThrowingInterceptor : IInterceptor
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();

		public bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState,
														 string[] propertyNames, IType[] types)
		{
			_Logger.Debug("throwing validation exception");

			throw new ApplicationException("imaginary validation error");
		}

		#region unused

		public bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			return false;
		}

		public int[] FindDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames,
													 IType[] types)
		{
			return null;
		}

		public object GetEntity(string entityName, object id)
		{
			return null;
		}

		public void AfterTransactionBegin(ITransaction tx)
		{
		}

		public void BeforeTransactionCompletion(ITransaction tx)
		{
		}

		public void AfterTransactionCompletion(ITransaction tx)
		{
		}

		public string GetEntityName(object entity)
		{
			return null;
		}

		public object Instantiate(string entityName, object id)
		{
			return null;
		}

		public bool? IsTransient(object entity)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionRecreate(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionRemove(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionUpdate(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void OnDelete(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			return false;
		}

		public void PostFlush(ICollection entities)
		{
		}

		public void PreFlush(ICollection entities)
		{
		}

		public void SetSession(ISession session)
		{
		}

		#endregion

		public SqlString OnPrepareStatement(SqlString sql)
		{
			return sql;
		}
	}


	public class Test
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();
		private readonly ISessionManager _SessionManager;
		private Guid _ThingId;

		public Test(ISessionManager sessionManager)
		{
			_SessionManager = sessionManager;
		}
		
		public virtual void Run()
		{
			_Logger.Debug("run invoked");

			SaveNewThing();
			try
			{
				_Logger.Debug("chaning thing which will throw");

				ChangeThing();
			}
			catch (TransactionAbortedException)
			{
				// this exception is expected - it is thrown by the validator
			}
			catch (TransactionException)
			{
				// this exception is expected - it is thrown by the validator
				// this exception is throw when using explicit transactions, because Commit action fails
			}

			// loading a new thing, in a new session!!
			var t = LoadThing();
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
			var s = _SessionManager.OpenSession();
			var thing = new Thing(18.0);
			_ThingId = (Guid)s.Save(thing);
		}

		[Transaction]
		protected virtual void SaveNewThingWithFollowingError()
		{
			using (var s = _SessionManager.OpenSession())
			{
				var thing = new Thing(37.0);
				_ThingId = (Guid)s.Save(thing);
				s.Flush();

				throw new InvalidOperationException("Artificial error after saving item");
			}
		}

		[Transaction]
		protected virtual void ChangeThing()
		{
			var s = _SessionManager.OpenSession();
			var thing = s.Load<Thing>(_ThingId);
			thing.Value = 19.0;
		}

		//Removed Transaction attribute, Transaction is not needed to load a thing
		protected virtual Thing LoadThing()
		{
			var s = _SessionManager.OpenSession(); // we are expecting this to be a new session
			return s.Load<Thing>(_ThingId);
		}

		[Transaction]
		protected virtual Thing GetThing()
		{
			var s = _SessionManager.OpenSession();
			return s.QueryOver<Thing>().Where(x => x.Id == _ThingId).SingleOrDefault();
		}
	}
}