// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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

using System.Diagnostics;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Lifestyles;
using DryIoc.Facilities.AutoTx.Utils;
using DryIoc.Transactions;
using DryIoc.Transactions.Activities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Facilities.AutoTx
{
	///<summary>
	///  <para>A facility for automatically handling transactions using the lightweight
	///    transaction manager. This facility does not depend on
	///    any other facilities.</para>
	///</summary>
	public class AutoTxFacility
	{
		public void Init(IContainer container)
		{
			ILogger _Logger = NullLogger.Instance;

			// check we have a logger factory
			if (container.IsRegistered(typeof (ILoggerFactory)))
			{
				// get logger factory
				var loggerFactory = container.Resolve<ILoggerFactory>();
				// get logger
				_Logger = loggerFactory.CreateLogger(typeof (AutoTxFacility));
			}
			else
			{
				Trace.TraceWarning("Missing ILogger in container; add it or you'll have no logging of errors!");
				container.UseInstance(typeof(ILoggerFactory), NullLoggerFactory.Instance);
			}

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("initializing AutoTxFacility");

			if (!container.IsRegistered(typeof(ILogger)))
			{
				container.AddLoggerResolving();

				if (_Logger.IsEnabled(LogLevel.Debug))
					_Logger.LogDebug("Added capability of resolving ILogger");
			}

			// add capability to inject info about requested service to the constructor
			container.Register<ProxyTypeStorage>(Reuse.Singleton);
			container.Register(Made.Of(
				() => new ParentServiceRequestInfo(Arg.Index<RequestInfo>(0), Arg.Of<ProxyTypeStorage>()), 
				request => request), setup: Setup.With(asResolutionCall: true));

			// register PerTransactionScopeContext to container as singleton (one storage for scope-per-transaction)
			container.Register<PerTransactionScopeContext>(Reuse.Singleton);
			container.Register<PerTopTransactionScopeContext>(Reuse.Singleton);

			// the interceptor needs to be created for every method call
			container.Register<TransactionInterceptor>(Reuse.Transient);
			container.Register<ITransactionMetaInfoStore, TransactionClassMetaInfoStore>(Reuse.Singleton);
			container.RegisterMany(new[] {typeof(ITransactionManager), typeof(TransactionManager)}, typeof(TransactionManager), Reuse.Singleton);

			// the activity manager shouldn't have the same lifestyle as TransactionInterceptor, as it
			// calls a static .Net/Mono framework method, and it's the responsibility of
			// that framework method to keep track of the call context.
			container.Register<IActivityManager, AsyncLocalActivityManager>(Reuse.Singleton);

			var componentInspector = new TransactionalComponentInspector(container);
			
			_Logger.LogDebug(
				"inspecting previously registered components; this might throw if you have configured your components in the wrong way");

			foreach (var serviceRegistrationInfo in container.GetServiceRegistrations())
			{
				componentInspector.ProcessModel(serviceRegistrationInfo);
			}

			container.Register<AutoTxFacility>(Reuse.Transient); // to determine if AutoTx was initialized

			_Logger.LogDebug(
				@"Initialized AutoTxFacility:

If you are experiencing problems, go to https://github.com/castleproject/Castle.Transactions and file a ticket for the Transactions project.
You can enable verbose logging for .Net by adding this to you .config file:

	<system.diagnostics>
		<sources>
			<source name=""System.Transactions"" switchValue=""Information"">
				<listeners>
					<add name=""tx"" type=""Castle.Transactions.Logging.TraceListener, Castle.Transactions""/>
				</listeners>
			</source>
		</sources>
	</system.diagnostics>

If you wish to e.g. roll back a transaction from within a transactional method you can resolve/use the ITransactionManager's
CurrentTransaction property and invoke Rollback on it. Be ready to catch TransactionAbortedException from the caller. You can enable
debugging through log4net.
");
		}
	}
}