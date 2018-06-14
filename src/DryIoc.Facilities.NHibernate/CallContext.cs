using System.Collections.Concurrent;
using System.Threading;

namespace DryIoc.Facilities.NHibernate
{
    internal static class CallContext
    {
        private static readonly ConcurrentDictionary<string, ThreadLocal<object>> _State = new ConcurrentDictionary<string, ThreadLocal<object>>();

        public static void SetData(string name, object data)
        {
            _State.GetOrAdd(name, _ => new ThreadLocal<object>()).Value = data;
        }

        public static object GetData(string name)
        {
            return _State.TryGetValue(name, out var data) ? data.Value : null;
        }
    }
}
