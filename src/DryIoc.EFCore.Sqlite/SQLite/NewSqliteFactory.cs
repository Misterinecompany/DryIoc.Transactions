using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DryIoc.EFCore.Sqlite.SQLite
{
	public class NewSqliteFactory : DbProviderFactory
	{
		private NewSqliteFactory()
		{
		}

		/// <summary>
		///     The singleton instance.
		/// </summary>
		public static readonly NewSqliteFactory Instance = new NewSqliteFactory();

		/// <summary>
		///     Creates a new command.
		/// </summary>
		/// <returns>The new command.</returns>
		public override DbCommand CreateCommand()
			=> new SqliteCommand();

		/// <summary>
		///     Creates a new connection.
		/// </summary>
		/// <returns>The new connection.</returns>
		public override DbConnection CreateConnection()
			=> new NewSqliteConnection();

		/// <summary>
		///     Creates a new connection string builder.
		/// </summary>
		/// <returns>The new connection string builder.</returns>
		public override DbConnectionStringBuilder CreateConnectionStringBuilder()
			=> new SqliteConnectionStringBuilder();

		/// <summary>
		///     Creates a new parameter.
		/// </summary>
		/// <returns>The new parameter.</returns>
		public override DbParameter CreateParameter()
			=> new SqliteParameter();
	}
}