﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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
using DryIoc.Facilities.EFCore.Tests.Extensions;
using DryIoc.Facilities.EFCore.Tests.Framework;
using DryIoc.Facilities.EFCore.Tests.TestClasses;
using DryIoc.Facilities.EFCore.Tests.TestClasses.Services;
using DryIoc.Transactions;
using NLog;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests
{
	public class SimpleUseCase_ProtectedMethod : EnsureSchema
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private Container c;

		[SetUp]
		public void SetUp()
		{
			c = GetContainer();
		}

		[TearDown]
		public void TearDown()
		{
			logger.Debug("running tear-down, removing components");

			using (var s = c.ResolveScope<TearDownService>())
			{
				s.Service.ClearThings();
			}

			c.Dispose();
		}

		private static Container GetContainer()
		{
			var c = new Container();
			c.AddNLogLogging();
			c.Register<IEFCoreInstaller, ExampleInstaller>(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArguments);
			c.Register<TearDownService>(Reuse.Transient);

			// Adding AutoTx and EFCore must be done after all components are initialized

			return c;
		}

		[Test]
		public void Register_Run()
		{
			c.Register<ServiceWithProtectedMethodInTransaction>(Reuse.Singleton);

			c.AddAutoTx();
			c.AddEFCore();

			Assert.That(c.IsRegistered(typeof(ITransactionManager)));

			using (var s = c.ResolveScope<ServiceWithProtectedMethodInTransaction>())
			{
				s.Service.Do();
			}
		}
	}
}