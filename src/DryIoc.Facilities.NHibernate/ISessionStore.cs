using DryIoc.Facilities.NHibernate.UnitOfWork;
using NHibernate;

namespace DryIoc.Facilities.NHibernate
{
	public interface ISessionStore
	{
		void SetData(IUnitOfWork data);
		IUnitOfWork GetData();
		IUnitOfWork GetAndClearData();
	}
}