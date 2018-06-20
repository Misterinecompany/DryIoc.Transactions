using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	public interface IDbContextStore
	{
		void SetData(DbContext data);
		DbContext GetData();
		void ClearData();
	}
}