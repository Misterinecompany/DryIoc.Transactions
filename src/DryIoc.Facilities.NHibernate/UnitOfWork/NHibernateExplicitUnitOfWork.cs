using System;
using NHibernate;

namespace DryIoc.Facilities.NHibernate.UnitOfWork
{
	public class NHibernateExplicitUnitOfWork : IUnitOfWork
	{
		private readonly ISession _Session;
		private ITransaction _Transaction;

		public NHibernateExplicitUnitOfWork(ISession session)
		{
			_Session = session;
			_Transaction = _Session.BeginTransaction();
		}

		public ISession CurrentSession => _Session;

		public void Dispose()
		{
			if (_Transaction != null)
			{
				Rollback();
			}
		}

		public void Commit()
		{
			if (_Transaction == null)
				throw new InvalidOperationException("UnitOfWork have already been closed.");

			try
			{
				if (_Transaction.IsActive)
				{
					_Transaction.Commit();
				}
			}
			catch
			{
				if (_Transaction.IsActive)
				{
					_Transaction.Rollback();
				}
				throw;
			}
			finally
			{
				_Transaction = null;
				CurrentSession.Dispose();
			}
		}

		public void Rollback()
		{
			if (_Transaction == null)
				throw new InvalidOperationException("UnitOfWork have already been closed.");

			try
			{
				if (_Transaction.IsActive)
				{
					_Transaction.Rollback();
				}
			}
			finally
			{
				_Transaction = null;
				CurrentSession.Dispose();
			}
		}
	}
}