using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore.Tests.TestClasses
{
	public class ExampleDbContext : DbContext
	{
		public ExampleDbContext(DbContextOptions options) : base(options)
		{
		}

		public DbSet<EfcThing> Things { get; set; }
	}

	public class ExampleDbContextManager : DbContextManager<ExampleDbContext>
	{
		public ExampleDbContextManager(IDbContextManager dbContextManager) : base(dbContextManager)
		{
		}
	}
}