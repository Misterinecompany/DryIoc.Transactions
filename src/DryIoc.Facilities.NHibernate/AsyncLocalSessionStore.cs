using System.Threading;
using DryIoc.Facilities.NHibernate.UnitOfWork;

namespace DryIoc.Facilities.NHibernate
{
	public class AsyncLocalSessionStore : ISessionStore
	{
		private readonly AsyncLocal<IUnitOfWork> _AsyncLocalSession = new AsyncLocal<IUnitOfWork>();

		public void SetData(IUnitOfWork data)
		{
			_AsyncLocalSession.Value = data;
		}

		public IUnitOfWork GetData()
		{
			return _AsyncLocalSession.Value;
		}

		public IUnitOfWork GetAndClearData()
		{
			var data = _AsyncLocalSession.Value;
			_AsyncLocalSession.Value = null;
			return data;
		}
	}
}
