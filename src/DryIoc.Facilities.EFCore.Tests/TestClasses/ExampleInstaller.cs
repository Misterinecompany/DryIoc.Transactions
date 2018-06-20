using System;
using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore.Tests.TestClasses
{
	internal class ExampleInstaller : IEFCoreInstaller
	{
		public const string Key = "default";

		public bool IsDefault => true;

		public string DbContextFactoryKey => Key;

		public Type DbContextImplementationType => typeof(ExampleDbContext);

		public DbContextOptionsBuilder Config => new DbContextOptionsBuilder()
			.UseSqlite("Data Source=DataStore.db");

		public void Registered()
		{
		}
	}
}
