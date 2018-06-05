#region license

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

#endregion

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Castle.Facilities.AutoTx.Lifestyles
{
	/*
	/// <summary>
	/// 	Abstract hybrid lifestyle manager, with two underlying lifestyles
	/// </summary>
	/// <typeparam name = "T">Primary lifestyle manager which has its constructor resolved through
	/// 	the main kernel.</typeparam>
	public class WrapperResolveLifestyleManager<T> : AbstractLifestyleManager
		where T : class, ILifestyleManager
	{
		private ILogger _Logger;

		private readonly IKernel _LifestyleKernel = new DefaultKernel();
		protected T _Lifestyle1;
		private bool _Disposed;

		[ContractPublicPropertyName("Initialized")] private bool _Initialized;

		public bool Initialized
		{
			get { return _Initialized; }
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(!Initialized || _Lifestyle1 != null);
		}

		public override void Init(IComponentActivator componentActivator, IKernel kernel, ComponentModel model)
		{
			Contract.Ensures(_Lifestyle1 != null);
			Contract.Ensures(Initialized);

			// check ILoggerFactory is registered
			if (kernel.HasComponent(typeof(ILoggerFactory)))
			{
				// get logger factory instance
				var loggerFactory = kernel.Resolve<ILoggerFactory>();
				// create logger
				_Logger = loggerFactory.CreateLogger(typeof(WrapperResolveLifestyleManager<T>));
			}
			else
				_Logger = NullLogger.Instance;

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("initializing (for component: {0})", string.Join(",", model.Services));

			_LifestyleKernel.Register(Component.For<T>().LifeStyle.Transient.Named("T.lifestyle"));
			kernel.AddChildKernel(_LifestyleKernel);

			try
			{
				_Lifestyle1 = _LifestyleKernel.Resolve<T>();
			}
			finally
			{
				kernel.RemoveChildKernel(_LifestyleKernel);
			}

			_Lifestyle1.Init(componentActivator, kernel, model);

			base.Init(componentActivator, kernel, model);

			Contract.Assume(_Lifestyle1 != null,
			                "lifestyle1 can't be null because the Resolve<T> call will throw an exception if a matching service wasn't found");

			_Logger.LogDebug("initialized");

			_Initialized = true;
		}

		public override bool Release(object instance)
		{
			Contract.Requires(Initialized);
			return _Lifestyle1.Release(instance);
		}

		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly",
			Justification = "I can't make it public and 'sealed'/non inheritable, as I'm overriding it")]
		public override void Dispose()
		{
			Contract.Ensures(!Initialized);

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool managed)
		{
			Contract.Ensures(!managed || !Initialized);

			if (!managed)
				return;

			if (_Disposed)
			{
				if (_Logger.IsEnabled(LogLevel.Information))
				{
					_Logger.LogInformation("repeated call to Dispose. will show stack-trace in debug mode next. this method call is idempotent");

					if (_Logger.IsEnabled(LogLevel.Debug))
						_Logger.LogDebug(new StackTrace().ToString());
				}

				_Initialized = false;
				return;
			}

			try
			{
				_LifestyleKernel.ReleaseComponent(_Lifestyle1);
				_LifestyleKernel.Dispose();
				_Lifestyle1 = null;
			}
			finally
			{
				_Disposed = true;
				_Initialized = false;
			}
		}

		public override object Resolve(CreationContext context, IReleasePolicy releasePolicy)
		{
			Contract.Requires(Initialized);
			Contract.Ensures(Contract.Result<object>() != null);
			var resolve = _Lifestyle1.Resolve(context, releasePolicy);
			Contract.Assume(resolve != null, "the resolved instance shouldn't be null");
			return resolve;
		}
	}
	*/
}