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
using DryIoc.Facilities.EFCore.Tests.TestClasses.Entities;
using DryIoc.Transactions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DryIoc.Facilities.EFCore.Tests.TestClasses.Services
{
	public class ServiceWithProtectedMethodInTransaction
	{
		private readonly DbContextOptions _dbContextOptions;

		public ServiceWithProtectedMethodInTransaction(DbContextOptions dbContextOptions)
		{
			_dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
		}

		public void Do()
		{
			var id = SaveIt();
			ReadAgain(id);
		}

		protected void ReadAgain(Guid id)
		{
			using (var dbContext = new ExampleDbContext(_dbContextOptions))
			{
				var t = dbContext.Find<EfcThing>(id);
				Assert.That(t.Id, Is.EqualTo(id));
			}
		}

		[Transaction]
		protected virtual Guid SaveIt()
		{
			using (var dbContext = new ExampleDbContext(_dbContextOptions))
			{
				var newThing = new EfcThing
				{
					Value = 45.0
				};
				dbContext.Add(newThing);
				dbContext.SaveChanges();

				return newThing.Id;
			}
		}
	}
}