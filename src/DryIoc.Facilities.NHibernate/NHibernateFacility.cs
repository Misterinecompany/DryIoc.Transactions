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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using DryIoc.Facilities.AutoTx;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.NHibernate.Errors;
using DryIoc.Transactions;
using DryIoc.Transactions.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NHibernate;
using NHibernate.Cfg;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace DryIoc.Facilities.NHibernate
{
	///<summary>
	///	Easy NHibernate integration with declarative transactions 
	///	using Castle Transaction Services and .Net System.Transactions.
	///	Integrate Transactional NTFS with NHibernate and database transactions, 
	///	or choose methods to fork dependent transactions for to run your transaction 
	///	constituents in parallel. The NHibernate Facility is configured 
	///	using FluentNHibernate
	///</summary>
	public class NHibernateFacility
	{
		private ILogger _Logger = NullLogger.Instance;
		private DefaultSessionLifeStyleOption _DefaultLifeStyle;
		private FlushMode _FlushMode;
		private readonly AmbientTransactionOption _AmbientTransaction;

		/// <summary>
		/// 	The suffix on the name of the component that has a lifestyle of Per Transaction.
		/// </summary>
		public const string SessionPerTxSuffix = "-session";

		///<summary>
		///	The suffix on the name of the ISession/component that has a lifestyle of Per Web Request.
		///</summary>
		public const string SessionPWRSuffix = "-session-pwr";

		/// <summary>
		/// 	The suffix on the name of the ISession/component that has a transient lifestyle.
		/// </summary>
		public const string SessionTransientSuffix = "-session-transient";

		/// <summary>
		/// 	The suffix of the session manager component.
		/// </summary>
		public const string SessionManagerSuffix = "-manager";

		/// <summary>
		/// 	The infix (fackey-[here]-session) of stateless session in the naming of components
		/// 	in Windsor.
		/// </summary>
		public const string SessionStatelessInfix = "-stateless";


		/// <summary>
		/// 	Instantiates a new NHibernateFacility with the default options.
		/// </summary>
		/// <param name = "defaultLifeStyle">The default session life style option </param>
		/// <param name = "flushMode">The session flush mode</param>
		/// <param name = "ambientTransaction">Configure how to handle transactions</param>
		public NHibernateFacility(DefaultSessionLifeStyleOption defaultLifeStyle = DefaultSessionLifeStyleOption.SessionPerTransaction,
			FlushMode flushMode = FlushMode.Auto, AmbientTransactionOption ambientTransaction = AmbientTransactionOption.Disabled)
		{
			_DefaultLifeStyle = defaultLifeStyle;
			_FlushMode = flushMode;
			_AmbientTransaction = ambientTransaction;
		}

		/// <summary>
		/// 	Gets or sets the default session life style option.
		/// </summary>
		public DefaultSessionLifeStyleOption DefaultLifeStyle
		{
			get { return _DefaultLifeStyle; }
			set { _DefaultLifeStyle = value; }
		}

		/// <summary>
		/// 	Gets or sets the default nhibernate session flush mode. This
		/// 	mode does not apply to stateless sessions.
		/// </summary>
		public FlushMode FlushMode
		{
			get { return _FlushMode; }
			set { _FlushMode = value; }
		}

		///<summary>
		///	Initialize, override. Registers everything relating to NHibernate in the container, including:
		///	<see cref = "ISessionFactory" />, <see cref = "ISessionManager" />, <see cref = "Func{TResult}" />, <see
		/// 	cref = "Configuration" />,
		///	<see cref = "ISession" />, <see cref = "IStatelessSession" />.
		///</summary>
		///<remarks>
		///	Requires <see cref = "TypedFactoryFacility" /> and <see cref = "FactorySupportFacility" /> which will be registered by this
		///	facility if there are none already registered.
		///</remarks>
		///<exception cref = "FacilityException">
		///	If any of:
		///	<list type = "bullet">
		///		<item>You haven't added <see cref = "AutoTxFacility" />.</item>
		///		<item>no <see cref = "INHibernateInstaller" /> components registered</item>
		///		<item>one or many of the <see cref = "INHibernateInstaller" /> components had a null or empty session factory key returned</item>
		///		<item>zero or more than one of the <see cref = "INHibernateInstaller" /> components had <see
		/// 	cref = "INHibernateInstaller.IsDefault" /> returned as true</item>
		///		<item>duplicate <see cref = "INHibernateInstaller.SessionFactoryKey" />s registered</item>
		///	</list>
		///</exception>
		[ContractVerification(false)] // interactive bits don't have contracts
		public void Init(IContainer container)
		{
			// check we have a logger factory
			if (container.IsRegistered(typeof(ILoggerFactory)))
			{
				// get logger factory
				var loggerFactory = container.Resolve<ILoggerFactory>();
				// get logger
				_Logger = loggerFactory.CreateLogger(typeof(NHibernateFacility));
			}

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("initializing NHibernateFacility");

			if (!container.IsRegistered(typeof(IConfigurationPersister)))
			{
				container.Register<IConfigurationPersister, FileConfigurationPersister>();
			}

			container.Register<ISessionStore, AsyncLocalSessionStore>(Reuse.Transient);

			var installers = container.ResolveMany<INHibernateInstaller>().ToList();

			Contract.Assume(installers != null, "ResolveAll shouldn't return null");

			if (installers.Count == 0)
				throw new NHibernateFacilityException("no INHibernateInstaller-s registered.");

			var count = installers.Count(x => x.IsDefault);
			if (count == 0 || count > 1)
				throw new NHibernateFacilityException("no INHibernateInstaller has IsDefault = true or many have specified it");

			if (installers.Any(x => string.IsNullOrEmpty(x.SessionFactoryKey)))
				throw new NHibernateFacilityException("all session factory keys must be non null and non empty strings");

			container.AssertHasFacility<AutoTxFacility>();

			var autoTxOptions = container.Resolve<AutoTxOptions>();
			autoTxOptions.AmbientTransaction = _AmbientTransaction; // override setting by local configuration

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("registering facility components");

			var added = new HashSet<string>();

			var isDefaultRegistered = false;
			var installed = installers
				.Select(x =>
				{
					Configuration configuration = x.Deserialize();

					if (configuration == null)
					{
                        configuration = x.Config;
						x.Serialize(configuration);
					}

					x.AfterDeserialize(configuration);

					return new
					{
						Config = configuration,
						Instance = x
					};

				})
				.Select(x => new Data { Config = x.Config, Instance = x.Instance, Factory = x.Config.BuildSessionFactory() })
				.OrderByDescending(x => x.Instance.IsDefault)
				.Do(x =>
				{
					if (!added.Add(x.Instance.SessionFactoryKey))
						throw new NHibernateFacilityException(
							$"Duplicate session factory keys '{x.Instance.SessionFactoryKey}' added. Verify that your INHibernateInstaller instances are not named the same.");
				})
				.Do(x =>
				{
					if (x.Instance.IsDefault && isDefaultRegistered)
					{
						throw new InvalidOperationException("Can't register more than one default NHibernateFacility");
					}
					
					container.UseInstance(x.Config, serviceKey: $"{x.Instance.SessionFactoryKey}-cfg");
					container.UseInstance(x.Factory, serviceKey: x.Instance.SessionFactoryKey);

					RegisterSession(container, x, 0, x.Instance.IsDefault);
					RegisterSession(container, x, 1);
					RegisterSession(container, x, 2);
					RegisterStatelessSession(container, x, 0, x.Instance.IsDefault);
					RegisterStatelessSession(container, x, 1);
					RegisterStatelessSession(container, x, 2);

					container.Register<ISessionManager>(Reuse.Singleton,
						Made.Of(() => new SessionManager(Arg.Index<Func<ISession>>(0), Arg.Of<ITransactionManager>(), Arg.Of<ISessionStore>(), Arg.Of<AutoTxOptions>()),
							request =>
							{
								var factory = container.Resolve<ISessionFactory>(x.Instance.SessionFactoryKey);
								return new Func<ISession>(() => CreateSession(factory, x, _FlushMode));
								//return new Func<ISession>(() =>
								//{
								//	var s = x.Instance.Interceptor.Do(y => factory.WithOptions().Interceptor(y).OpenSession())
								//		.OrDefault(factory.OpenSession());
								//	s.FlushMode = flushMode;
								//	return s;
								//});
							}),
						serviceKey: x.Instance.SessionFactoryKey + SessionManagerSuffix);

					if (x.Instance.IsDefault)
					{
						container.UseInstance(x.Config);
						container.UseInstance(x.Factory);
						container.RegisterMapping<ISessionManager, ISessionManager>(registeredServiceKey: x.Instance.SessionFactoryKey + SessionManagerSuffix);

						isDefaultRegistered = true;
					}

					//container.Register(
						//Component.For<Configuration>()
						//	.Instance(x.Config)
						//	.LifeStyle.Singleton
						//	.Named(x.Instance.SessionFactoryKey + "-cfg"),
						//Component.For<ISessionFactory>()
						//	.Instance(x.Factory)
						//	.LifeStyle.Singleton
						//	.Named(x.Instance.SessionFactoryKey),
						//RegisterSession(x, 0),
						//RegisterSession(x, 1),
						//RegisterSession(x, 2),
						//RegisterStatelessSession(x, 0),
						//RegisterStatelessSession(x, 1),
						//RegisterStatelessSession(x, 2),

						//Component.For<ISessionManager>().Instance(new SessionManager(() =>
						//	{
						//		var factory = container.Resolve<ISessionFactory>(x.Instance.SessionFactoryKey);
						//		var s = x.Instance.Interceptor.Do(y => factory.WithOptions().Interceptor(y).OpenSession())
						//			.OrDefault(factory.OpenSession());
						//		s.FlushMode = flushMode;
						//		return s;
						//	}, container.Resolve<ITransactionManager>()))
						//	.Named(x.Instance.SessionFactoryKey + SessionManagerSuffix)
						//	.LifeStyle.Singleton);
				})
				.ToList();

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("notifying the nhibernate installers that they have been configured");

			installed.Run(x => x.Instance.Registered(x.Factory));

			if (_Logger.IsEnabled(LogLevel.Debug))
				_Logger.LogDebug("Initialized NHibernateFacility");
		}

		private void RegisterStatelessSession(IContainer container, Data x, uint index, bool registerAsDefault = false)
		{
			Contract.Requires(index < 3,
							  "there are only three supported lifestyles; per transaction, per web request and transient");
			Contract.Requires(x != null);
			//Contract.Ensures(Contract.Result<IRegistration>() != null);

			//var registration = Component.For<IStatelessSession>()
			//	.UsingFactoryMethod(k => k.Resolve<ISessionFactory>(x.Instance.SessionFactoryKey).OpenStatelessSession());

			var sessionFactoryKey = x.Instance.SessionFactoryKey;
			var nameAndLifeStyle = GetNameAndLifeStyle(index, sessionFactoryKey + SessionStatelessInfix);

			container.Register<IStatelessSession>(nameAndLifeStyle.Item2,
				Made.Of(() => Arg.Of<ISessionFactory>(sessionFactoryKey).OpenStatelessSession()),
				serviceKey: nameAndLifeStyle.Item1);

			if (registerAsDefault)
			{
				container.RegisterMapping<IStatelessSession, IStatelessSession>(registeredServiceKey: nameAndLifeStyle.Item1);
			}
		}

		private static ISession CreateSession(ISessionFactory factory, Data x, FlushMode flushMode)
		{
			//var factory = k.Resolve<ISessionFactory>(x.Instance.SessionFactoryKey);
			var s = x.Instance.Interceptor.Do(y => factory.WithOptions().Interceptor(y).OpenSession())
				.OrDefault(factory.OpenSession());
			s.FlushMode = flushMode;
			//logger.DebugFormat("resolved session component named '{0}'", c.Handler.ComponentModel.Name);
			return s;
		}

		private void RegisterSession(IContainer container, Data x, uint index, bool registerAsDefault = false)
		{
			Contract.Requires(index < 3,
							  "there are only three supported lifestyles; per transaction, per web request and transient");
			Contract.Requires(x != null);
			//Contract.Ensures(Contract.Result<IRegistration>() != null);

			//var registration = Component.For<ISession>()
			//	.UsingFactoryMethod((k, c) =>
			//	{
			//		var factory = k.Resolve<ISessionFactory>(x.Instance.SessionFactoryKey);
			//		var s = x.Instance.Interceptor.Do(y => factory.WithOptions().Interceptor(y).OpenSession())
			//			.OrDefault(factory.OpenSession());
			//		s.FlushMode = flushMode;
			//		logger.DebugFormat("resolved session component named '{0}'", c.Handler.ComponentModel.Name);
			//		return s;
			//	});

			var localFlushMode = _FlushMode;
			var sessionFactoryKey = x.Instance.SessionFactoryKey;
			var nameAndLifeStyle = GetNameAndLifeStyle(index, sessionFactoryKey);

			container.Register<ISession>(nameAndLifeStyle.Item2,
				Made.Of(() => CreateSession(Arg.Of<ISessionFactory>(sessionFactoryKey), x, localFlushMode)),
				serviceKey: nameAndLifeStyle.Item1);

			if (registerAsDefault)
			{
				container.RegisterMapping<ISession, ISession>(registeredServiceKey: nameAndLifeStyle.Item1);
			}
		}

		private Tuple<string, IReuse> GetNameAndLifeStyle(uint index, string baseName)
		{
			Contract.Requires(index < 3,
							  "there are only three supported lifestyles; per transaction, per web request and transient");
			Contract.Ensures(Contract.Result<Tuple<string, IReuse>>() != null);

			switch (_DefaultLifeStyle)
			{
				case DefaultSessionLifeStyleOption.SessionPerTransaction:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + SessionPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + SessionPWRSuffix, Reuse.InWebRequest);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + SessionTransientSuffix, Reuse.Transient);
					break;
				case DefaultSessionLifeStyleOption.SessionPerWebRequest:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + SessionPWRSuffix, Reuse.InWebRequest);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + SessionPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + SessionTransientSuffix, Reuse.Transient);
					break;
				case DefaultSessionLifeStyleOption.SessionTransient:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + SessionTransientSuffix, Reuse.Transient);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + SessionPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + SessionPWRSuffix, Reuse.InWebRequest);
					break;
				default:
					throw new NHibernateFacilityException("Unknown default life style - please file a bug report");
			}
			throw new NHibernateFacilityException("Invalid index passed to GetNameAndLifeStyle<T> - please file a bug report");
		}

		private class Data
		{
			public INHibernateInstaller Instance;
			public Configuration Config;
			public ISessionFactory Factory;
		}
	}
}