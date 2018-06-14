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
using DryIoc.Facilities.NHibernate.Errors;
using DryIoc.Facilities.NHibernate.Tests.Extensions;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using DryIoc.Transactions;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	internal class Init
	{
		[Test]
		public void given_two_configs_resolves_the_default_true_one_first()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<INHibernateInstaller, C1>(Reuse.Singleton);
			c.Register<INHibernateInstaller, C2>(Reuse.Singleton);
			AssertOrder(c);
		}

		[Test]
		public void given_two_configs_resolves_the_default_true_one_first_permutate()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<INHibernateInstaller, C2>(Reuse.Singleton);
			c.Register<INHibernateInstaller, C1>(Reuse.Singleton);
			AssertOrder(c);
		}

		[Test]
		public void facility_exception_cases()
		{
			var c = GetTxContainer();
			try
			{
				c.AddNHibernate();
				Assert.Fail();
			}
			catch (NHibernateFacilityException ex)
			{
				Assert.That(ex.Message, Does.Contain("registered"));
			}
		}

		[Test]
		public void facility_exception_cases_no_default()
		{
			var c = GetTxContainer();

			c.Register<INHibernateInstaller, C2>(Reuse.Singleton);
			try
			{
				c.AddNHibernate();
				Assert.Fail();
			}
			catch (NHibernateFacilityException ex)
			{
				Assert.That(ex.Message, Does.Contain("IsDefault"));
			}
		}

		[Test]
		public void facility_exception_duplicate_keys()
		{
			var c = GetTxContainer();

			c.Register<INHibernateInstaller, C1>(Reuse.Singleton);
			c.Register<INHibernateInstaller, C1_Copy>(Reuse.Singleton);
			try
			{
				c.AddNHibernate();
				Assert.Fail();
			}
			catch (NHibernateFacilityException ex)
			{
				Assert.That(ex.Message.ToLowerInvariant(), Does.Contain("duplicate"));
			}
		}

		private static Container GetTxContainer()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.AddAutoTx();
			return c;
		}

		private void AssertOrder(Container c)
		{
			c.AddAutoTx();

			try
			{
				c.AddNHibernate();
				Assert.Fail("no exception thrown");
			}
			catch (ApplicationException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("C1"));
			}
		}

		#region Installers

		private class C1 : INHibernateInstaller
		{
			public bool IsDefault
			{
				get { return true; }
			}

			public string SessionFactoryKey
			{
				get { return "C1"; }
			}

			public Maybe<IInterceptor> Interceptor
			{
				get { return Maybe.None<IInterceptor>(); }
			}

			public Configuration Config
			{
				get { return new ExampleInstaller().Config; }
			}

			public void Registered(ISessionFactory factory)
			{
				throw new ApplicationException("C1");
			}

			public Configuration Deserialize()
			{
				return null;
			}

			public void Serialize(Configuration configuration)
			{
			}

			public void AfterDeserialize(Configuration configuration)
			{
			}
		}

		private class C2 : INHibernateInstaller
		{
			public bool IsDefault
			{
				get { return false; }
			}

			public string SessionFactoryKey
			{
				get { return "C2"; }
			}

			public Maybe<IInterceptor> Interceptor
			{
				get { return Maybe.None<IInterceptor>(); }
			}

			public Configuration Config
			{
				get { return new ExampleInstaller().Config; }
			}

			public void Registered(ISessionFactory factory)
			{
				throw new ApplicationException("C2");
			}

            public Configuration Deserialize()
            {
                return null;
            }

            public void Serialize(Configuration configuration)
            {
            }

            public void AfterDeserialize(Configuration configuration)
            {
            }
		}

		private class C1_Copy : INHibernateInstaller
		{
			public bool IsDefault
			{
				get { return false; }
			}

			public string SessionFactoryKey
			{
				get { return "C1"; }
			}

			public Maybe<IInterceptor> Interceptor
			{
				get { return Maybe.None<IInterceptor>(); }
			}

			public Configuration Config
			{
				get { return new ExampleInstaller().Config; }
			}

			public void Registered(ISessionFactory factory)
			{
				throw new ApplicationException("C1");
			}

            public Configuration Deserialize()
            {
                return null;
            }

            public void Serialize(Configuration configuration)
            {
            }

            public void AfterDeserialize(Configuration configuration)
            {
            }
		}

		#endregion
	}
}