using System.Collections.Concurrent;
using System.Threading;
using NHibernate;

namespace DryIoc.Facilities.NHibernate
{
	public class AsyncLocalSessionStore
	{
		private readonly AsyncLocal<ConcurrentDictionary<string, ISession>> _State = new AsyncLocal<ConcurrentDictionary<string, ISession>>();

		public void SetData(string sessionId, ISession data)
		{
			var dictionary = _State.Value;
			if (dictionary == null)
			{
				dictionary = new ConcurrentDictionary<string, ISession>();
				_State.Value = dictionary;
			}

			dictionary.TryAdd(sessionId, data);
		}

		public ISession GetData(string sessionId)
		{
			ISession data = null;
			_State.Value?.TryGetValue(sessionId, out data);
			return data;
		}

		public void ClearData(string sessionId)
		{
			var dictionary = _State.Value;
			if (dictionary == null)
			{
				return;
			}

			dictionary.TryRemove(sessionId, out _);
			if (dictionary.Count == 0)
			{
				_State.Value = null;
			}
		}
	}
}
