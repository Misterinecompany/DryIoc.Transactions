﻿#region license

// Copyright 2009-2011 Henrik Feldt - http://logibit.se/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Transactions;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Testing;
using DryIoc.Facilities.AutoTx.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.AutoTx.Tests
{
	public class SingleThread_Ambient_OnConcreteType
	{
		private Container _Container;

		[SetUp]
		public void SetUp()
		{
			_Container = new Container();
			_Container.Register<ConcreteService>(Reuse.Singleton);
			_Container.AddAutoTx();
		}

		[TearDown]
		public void TearDown()
		{
			_Container.Dispose();
		}

		[Test]
		public void NonRecursive()
		{
			using (var scope = new ResolveScope<ConcreteService>(_Container))
				scope.Service.VerifyInAmbient();
		}

		[Test]
		public void Recursive()
		{
			using (var scope = new ResolveScope<ConcreteService>(_Container))
			{
				scope.Service.VerifyInAmbient(() =>
					scope.Service.VerifyInAmbient(() => Assert.That(Transaction.Current != null 
																	&& Transaction.Current is DependentTransaction)
				));
			}
		}
	}
}