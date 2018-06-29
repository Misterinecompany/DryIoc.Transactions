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
using DryIoc.Facilities.EFCore.Errors;
using DryIoc.Facilities.EFCore.Tests.Extensions;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	internal class Init
	{
		[Test]
		public void given_two_configs_resolves_the_default_true_one_first()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<IEFCoreInstaller, C1>(Reuse.Singleton);
			c.Register<IEFCoreInstaller, C2>(Reuse.Singleton);
			AssertOrder(c);
		}

		[Test]
		public void given_two_configs_resolves_the_default_true_one_first_permutate()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<IEFCoreInstaller, C2>(Reuse.Singleton);
			c.Register<IEFCoreInstaller, C1>(Reuse.Singleton);
			AssertOrder(c);
		}

		[Test]
		public void facility_exception_cases()
		{
			var c = GetTxContainer();
			try
			{
				c.AddEFCore();
				Assert.Fail();
			}
			catch (EFCoreFacilityException ex)
			{
				Assert.That(ex.Message, Does.Contain("registered"));
			}
		}

		[Test]
		public void facility_exception_cases_no_default()
		{
			var c = GetTxContainer();

			c.Register<IEFCoreInstaller, C2>(Reuse.Singleton);
			try
			{
				c.AddEFCore();
				Assert.Fail();
			}
			catch (EFCoreFacilityException ex)
			{
				Assert.That(ex.Message, Does.Contain("IsDefault"));
			}
		}

		[Test]
		public void facility_exception_duplicate_keys()
		{
			var c = GetTxContainer();

			c.Register<IEFCoreInstaller, C1>(Reuse.Singleton);
			c.Register<IEFCoreInstaller, C1_Copy>(Reuse.Singleton);
			try
			{
				c.AddEFCore();
				Assert.Fail();
			}
			catch (EFCoreFacilityException ex)
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
				c.AddEFCore();
				Assert.Fail("no exception thrown");
			}
			catch (ApplicationException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("C1"));
			}
		}

		#region Installers

		private class C1 : IEFCoreInstaller
		{
			public bool IsDefault => true;

			public string DbContextFactoryKey => "C1";

			public Type DbContextImplementationType => typeof(ExampleDbContext);

			public Type TypedDbContextManagerType => null;

			public DbContextOptionsBuilder Config => new ExampleInstaller().Config;

			public TransactionCommitAction TransactionCommitAction => TransactionCommitAction.Dispose;

			public void Registered()
			{
				throw new ApplicationException("C1");
			}
		}

		private class C2 : IEFCoreInstaller
		{
			public bool IsDefault => false;

			public string DbContextFactoryKey => "C2";

			public Type DbContextImplementationType => typeof(ExampleDbContext);

			public Type TypedDbContextManagerType => null;

			public DbContextOptionsBuilder Config => new ExampleInstaller().Config;

			public TransactionCommitAction TransactionCommitAction => TransactionCommitAction.Dispose;

			public void Registered()
			{
				throw new ApplicationException("C2");
			}
		}

		private class C1_Copy : IEFCoreInstaller
		{
			public bool IsDefault => false;

			public string DbContextFactoryKey => "C1";

			public Type DbContextImplementationType => typeof(ExampleDbContext);

			public Type TypedDbContextManagerType => null;

			public DbContextOptionsBuilder Config => new ExampleInstaller().Config;

			public TransactionCommitAction TransactionCommitAction => TransactionCommitAction.Dispose;

			public void Registered()
			{
				throw new ApplicationException("C1");
			}
		}

		#endregion
	}
}