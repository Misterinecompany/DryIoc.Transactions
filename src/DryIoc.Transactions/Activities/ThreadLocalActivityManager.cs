﻿// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;
using Castle.Core.Logging;

namespace Castle.Transactions.Activities
{
	/// <summary>
	///   The ThreadLocal activity manager saves the stack of transactions in thread local variable. This is the recommended manager and the default, also.
	/// </summary>
	public class ThreadLocalActivityManager : IActivityManager
	{
		private static readonly ThreadLocal<Activity> _threadLocalActivity = new ThreadLocal<Activity>();

		public ThreadLocalActivityManager()
		{
			_threadLocalActivity.Value = null;
		}

		public Activity GetCurrentActivity()
		{
			var activity = _threadLocalActivity.Value;

			if (activity == null)
			{
				activity = new Activity(NullLogger.Instance);
				_threadLocalActivity.Value = activity;
			}

			return activity;
		}
	}
}