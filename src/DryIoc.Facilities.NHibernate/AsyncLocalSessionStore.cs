using System.Threading;
using NHibernate;

namespace DryIoc.Facilities.NHibernate
{
	public class AsyncLocalSessionStore : ISessionStore
	{
		private readonly AsyncLocal<ISession> _AsyncLocalSession = new AsyncLocal<ISession>();

		public void SetData(ISession data)
		{
			_AsyncLocalSession.Value = data;
		}

		public ISession GetData()
		{
			return _AsyncLocalSession.Value;
		}

		public ISession GetAndClearData()
		{
			var data = _AsyncLocalSession.Value;
			_AsyncLocalSession.Value = null;
			return data;
		}
	}
}
