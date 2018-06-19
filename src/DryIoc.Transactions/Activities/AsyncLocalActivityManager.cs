using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Transactions.Activities
{
	/// <summary>
	///   The AsyncLocal activity manager saves the stack of transactions in async local variable. This is the recommended manager and the default, also.
	/// </summary>
	public class AsyncLocalActivityManager : IActivityManager
	{
		private static readonly AsyncLocal<Activity> _AsyncLocalActivity = new AsyncLocal<Activity>();

		public AsyncLocalActivityManager()
		{
			_AsyncLocalActivity.Value = null;
		}

		public Activity GetCurrentActivity()
		{
			var activity = _AsyncLocalActivity.Value;

			if (activity == null)
			{
				activity = new Activity(NullLogger.Instance);
				_AsyncLocalActivity.Value = activity;
			}

			return activity;
		}

		public void CreateNewActivity()
		{
			var activity = new Activity(NullLogger.Instance);
			_AsyncLocalActivity.Value = activity;
		}
	}
}