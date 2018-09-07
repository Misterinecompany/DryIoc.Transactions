// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

using System.Diagnostics.Contracts;
using DryIoc.Transactions;
using NLog;

namespace DryIoc.Facilities.NHibernate.Tests.TestClasses
{
	public class TearDownService
	{
		private readonly ISessionManager _SessionManager;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public TearDownService(ISessionManager sessionManager)
		{
			Contract.Requires(sessionManager != null);
			_SessionManager = sessionManager;
		}

		[Transaction]
		public virtual void ClearThings()
		{
			logger.Debug("clearing things");
			_SessionManager.OpenSession().Delete("from Thing");
		}
	}
}