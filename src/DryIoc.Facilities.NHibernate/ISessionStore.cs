using NHibernate;

namespace DryIoc.Facilities.NHibernate
{
	public interface ISessionStore
	{
		void SetData(ISession data);
		ISession GetData();
		ISession GetAndClearData();
	}
}