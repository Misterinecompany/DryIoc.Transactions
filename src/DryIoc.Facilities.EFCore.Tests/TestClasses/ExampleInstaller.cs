using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DryIoc.Facilities.EFCore.Tests.TestClasses
{
	internal class ExampleInstaller : IEFCoreInstaller
	{
		private readonly ILoggerFactory _LoggerFactory;

		public ExampleInstaller(ILoggerFactory loggerFactory)
		{
			_LoggerFactory = loggerFactory;
		}

		public const string Key = "default";

		public bool IsDefault => true;

		public string DbContextFactoryKey => Key;

		public Type DbContextImplementationType => typeof(ExampleDbContext);

		public Type TypedDbContextManagerType => typeof(ExampleDbContextManager);

		public DbContextOptionsBuilder Config => new DbContextOptionsBuilder()
			.UseLoggerFactory(_LoggerFactory)
			//.UseSqliteWithIgnoredAmbientTransaction("Data Source=C:\\DataStore.db");
			.UseSqlServer("Server=localhost;Database=DryIocTransactionsTest;Trusted_Connection=True;");

		public TransactionCommitAction TransactionCommitAction => TransactionCommitAction.Dispose;

		public void Registered()
		{
		}
	}
}
