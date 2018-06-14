using System.Collections.Concurrent;
using System.Threading;

namespace Castle.Facilities.NHibernate
{
    internal static class CallContext
    {
        private static readonly ConcurrentDictionary<string, ThreadLocal<object>> state = new ConcurrentDictionary<string, ThreadLocal<object>>();

        public static void SetData(string name, object data)
        {
            state.GetOrAdd(name, _ => new ThreadLocal<object>()).Value = data;
        }

        public static object GetData(string name)
        {
            return state.TryGetValue(name, out ThreadLocal<object> data) ? data.Value : null;
        }
    }
}
