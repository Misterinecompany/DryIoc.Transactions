using NHibernate;

namespace DryIoc.Facilities.NHibernate.UnitOfWork
{
	public class NHibernateImplictUnitOfWork : IUnitOfWork
	{
		public NHibernateImplictUnitOfWork(ISession session)
		{
			CurrentSession = session;
		}

		public ISession CurrentSession { get; }

		public void Dispose()
		{
			CurrentSession.Dispose();
		}
		
		public void Commit()
		{
		}

		public void Rollback()
		{
		}
	}
}