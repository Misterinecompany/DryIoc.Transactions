using System.Threading;
using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	public class AsyncLocalDbContextStore : IDbContextStore
	{
		private readonly AsyncLocal<DbContext> _AsyncLocalDbContext = new AsyncLocal<DbContext>();

		public void SetData(DbContext data)
		{
			_AsyncLocalDbContext.Value = data;
		}

		public DbContext GetData()
		{
			return _AsyncLocalDbContext.Value;
		}

		public void ClearData()
		{
			_AsyncLocalDbContext.Value = null;
		}
	}
}
