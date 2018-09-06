using System;
using NHibernate;

namespace DryIoc.Facilities.NHibernate.UnitOfWork
{
	public interface IUnitOfWork : IDisposable
	{
		ISession CurrentSession { get; }

		void Commit();

		void Rollback();
	}
}
