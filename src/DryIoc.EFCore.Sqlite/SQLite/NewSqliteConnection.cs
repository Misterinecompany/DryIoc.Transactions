using System.Data.Common;
using System.Transactions;
using Microsoft.Data.Sqlite;

namespace DryIoc.EFCore.Sqlite.SQLite
{
	public class NewSqliteConnection : SqliteConnection
	{
		public NewSqliteConnection()
		{
		}

		public NewSqliteConnection(string connectionString) : base(connectionString)
		{
		}

		protected override DbProviderFactory DbProviderFactory
			=> NewSqliteFactory.Instance;

		public override void EnlistTransaction(Transaction transaction)
		{
			// do nothing
		}
	}
}