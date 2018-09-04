using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using DryIoc.Facilities.AutoTx;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.EFCore.Errors;
using DryIoc.Transactions;
using DryIoc.Transactions.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DryIoc.Facilities.EFCore
{
	///<summary>
	///	Easy Entity Framework Core integration with declarative transactions 
	///	using DryIoc Transaction Services and .Net System.Transactions.
	///	Integrate Transactional NTFS with EFCore and database transactions, 
	///	or choose methods to fork dependent transactions for to run your transaction 
	///	constituents in parallel.
	///</summary>
	public class EFCoreFacility
	{
		private ILogger logger = NullLogger.Instance;
		private readonly DefaultLifeStyleOption _defaultDbContextLifeStyle;

		/// <summary>
		/// 	The suffix on the name of the component that has a lifestyle of Per Transaction.
		/// </summary>
		public const string DbContextPerTxSuffix = "-context";

		///<summary>
		///	The suffix on the name of the DbContext/component that has a lifestyle of Per Web Request.
		///</summary>
		public const string DbContextPWRSuffix = "-context-pwr";

		/// <summary>
		/// 	The suffix on the name of the DbContext/component that has a transient lifestyle.
		/// </summary>
		public const string DbContextTransientSuffix = "-context-transient";

		/// <summary>
		/// 	The suffix of the DbContext manager component.
		/// </summary>
		public const string DbContextManagerSuffix = "-manager";

		/// <summary>
		/// 	Instantiates a new EFCoreFacility with the default options, DbContext per transaction.
		/// </summary>
		public EFCoreFacility() : this(DefaultLifeStyleOption.PerTransaction)
		{
		}

		/// <summary>
		/// 	Instantiates a new EFCoreFacility with a given lifestyle option.
		/// </summary>
		/// <param name = "defaultDbContextLifeStyle">The default DbContext life style.</param>
		public EFCoreFacility(DefaultLifeStyleOption defaultDbContextLifeStyle)
		{
			_defaultDbContextLifeStyle = defaultDbContextLifeStyle;
		}

		///<summary>
		///	Initialize, override. Registers everything relating to Entity Framework Core in the container, including:
		///	<see cref = "DbContext" />, <see cref = "IDbContextManager" />, <see cref = "Func{TResult}" />, <see
		/// 	cref = "DbContextOptions" />.
		///</summary>
		///<exception cref = "EFCoreFacilityException">
		///	If any of:
		///	<list type = "bullet">
		///		<item>You haven't added <see cref = "AutoTxFacility" />.</item>
		///		<item>no <see cref = "IEFCoreInstaller" /> components registered</item>
		///		<item>one or many of the <see cref = "IEFCoreInstaller" /> components had a null or empty DbContext factory key returned</item>
		///		<item>zero or more than one of the <see cref = "IEFCoreInstaller" /> components had <see
		/// 	cref = "IEFCoreInstaller.IsDefault" /> returned as true</item>
		///		<item>duplicate <see cref = "IEFCoreInstaller.DbContextFactoryKey" />s registered</item>
		///	</list>
		///</exception>
		public void Init(IContainer container)
		{
			// check we have a logger factory
			if (container.IsRegistered(typeof(ILoggerFactory)))
			{
				// get logger factory
				var loggerFactory = container.Resolve<ILoggerFactory>();
				// get logger
				logger = loggerFactory.CreateLogger(typeof(AutoTxFacility));
			}

			if (logger.IsEnabled(LogLevel.Debug))
				logger.LogDebug("initializing EFCoreFacility");

			container.Register<IDbContextStore, AsyncLocalDbContextStore>(Reuse.Transient);

			var installers = container.ResolveMany<IEFCoreInstaller>().ToList();

			Contract.Assume(installers != null, "ResolveAll shouldn't return null");

			if (installers.Count == 0)
				throw new EFCoreFacilityException("no IEFCoreInstaller-s registered.");

			var count = installers.Count(x => x.IsDefault);
			if (count == 0 || count > 1)
				throw new EFCoreFacilityException("no IEFCoreInstaller has IsDefault = true or many have specified it");

			if (installers.Any(x => string.IsNullOrEmpty(x.DbContextFactoryKey)))
				throw new EFCoreFacilityException("all DbContext factory keys must be non null and non empty strings");

			container.AssertHasFacility<AutoTxFacility>();

			if (logger.IsEnabled(LogLevel.Debug))
				logger.LogDebug("registering facility components");

			var added = new HashSet<string>();

			var isDefaultRegistered = false;
			var installed = installers
				.Select(x => new Data
				{
					Config = x.Config.Options,
					DbContextImplementationType = x.DbContextImplementationType,
					Instance = x
				})
				.OrderByDescending(x => x.Instance.IsDefault)
				.Do(x =>
				{
					// Validate EFCoreInstaller values

					if (!added.Add(x.Instance.DbContextFactoryKey))
						throw new EFCoreFacilityException(
							$"Duplicate DbContext factory keys '{x.Instance.DbContextFactoryKey}' added. Verify that your IEFCoreInstaller instances are not named the same.");

					CheckValidDbContextType(x.DbContextImplementationType);
					CheckValidDbContextConstructor(x.DbContextImplementationType);
					CheckValidDbContextManagerType(x.Instance.TypedDbContextManagerType);
				})
				.Do(x =>
				{
					if (x.Instance.IsDefault && isDefaultRegistered)
					{
						throw new InvalidOperationException("Can't register more than one default EFCoreFacility");
					}

					container.UseInstance(x.Config, serviceKey: $"{x.Instance.DbContextFactoryKey}-cfg");

					RegisterDbContext(container, x, 0, x.Instance.IsDefault);
					RegisterDbContext(container, x, 1);
					RegisterDbContext(container, x, 2);

					// Register DbContextManager for getting/creating DbContext by current context
					var dbContextServiceKey = x.Instance.DbContextFactoryKey + DbContextTransientSuffix;
					var transactionCommitAction = x.Instance.TransactionCommitAction;
					container.Register<IDbContextManager>(Reuse.Singleton,
						Made.Of(() => new DbContextManager(Arg.Of<Func<DbContext>>(dbContextServiceKey), Arg.Of<ITransactionManager>(), Arg.Of<IDbContextStore>(), transactionCommitAction)),
						serviceKey: x.Instance.DbContextFactoryKey + DbContextManagerSuffix);

					// Register typed version of DbContextManager (wrapper) for getting correctly casted DbContext
					if (x.Instance.TypedDbContextManagerType != null)
					{
						container.Register(x.Instance.TypedDbContextManagerType, Reuse.Singleton,
							serviceKey: x.Instance.DbContextFactoryKey + DbContextManagerSuffix);
					}
					
					// Register default services mapping (without serviceKey specification)
					if (x.Instance.IsDefault)
					{
						// Register default DbContextOptions and DbContextManager
						container.UseInstance(x.Config);
						container.RegisterMapping<IDbContextManager, IDbContextManager>(registeredServiceKey: x.Instance.DbContextFactoryKey + DbContextManagerSuffix);

						// Register default typed version of DbContextManager (wrapper)
						if (x.Instance.TypedDbContextManagerType != null)
						{
							container.RegisterMapping(x.Instance.TypedDbContextManagerType,
								x.Instance.TypedDbContextManagerType,
								registeredServiceKey: x.Instance.DbContextFactoryKey + DbContextManagerSuffix);
						}

						isDefaultRegistered = true;
					}
				})
				.ToList();

			if (logger.IsEnabled(LogLevel.Debug))
				logger.LogDebug("notifying the EFCore installers that they have been configured");

			installed.Run(x => x.Instance.Registered());

			if (logger.IsEnabled(LogLevel.Debug))
				logger.LogDebug("Initialized EFCoreFacility");
		}

		private static void CheckValidDbContextConstructor(Type dbContextImplementationType)
		{
			var constructor = dbContextImplementationType.PublicConstructors().FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(DbContextOptions)));
			if (constructor == null)
			{
				throw new EFCoreFacilityException($"Type {dbContextImplementationType} must contain constructor with parameter DbContextOptions which calls base constructor with this parameter");
			}
		}

		private static void CheckValidDbContextType(Type dbContextImplementationType)
		{
			if (!dbContextImplementationType.IsAssignableTo(typeof(DbContext)))
			{
				throw new EFCoreFacilityException($"Type {dbContextImplementationType} is not valid DbContext (it doesn't inherit from DbContext)");
			}
		}

		private static void CheckValidDbContextManagerType(Type typedDbContextManagerType)
		{
			if (typedDbContextManagerType != null && !typedDbContextManagerType.IsAssignableToGenericType(typeof(DbContextManager<>)))
			{
				throw new EFCoreFacilityException($"Type {typedDbContextManagerType} is not valid DbContextManager (it doesn't inherit from {typeof(DbContextManager<>)})");
			}
		}

		private void RegisterDbContext(IContainer container, Data x, uint index, bool registerAsDefault = false)
		{
			Contract.Requires(index < 3,
							  "there are only three supported lifestyles; per transaction, per web request and transient");
			Contract.Requires(x != null);

			var dbContextFactoryKey = x.Instance.DbContextFactoryKey;
			var nameAndLifeStyle = GetNameAndLifeStyle(index, dbContextFactoryKey);

			container.Register(x.DbContextImplementationType, nameAndLifeStyle.Item2,
				Made.Of(type => type.PublicConstructors().SingleOrDefault(c =>
					c.GetParameters().Any(p => p.ParameterType == typeof(DbContextOptions)))),
				Setup.With(allowDisposableTransient: true), // Transient disposing is handled by DbContextManager or must be hadled manually by user
				serviceKey: nameAndLifeStyle.Item1);

			container.RegisterMapping(typeof(DbContext), x.DbContextImplementationType, nameAndLifeStyle.Item1, nameAndLifeStyle.Item1);

			if (registerAsDefault)
			{
				container.RegisterMapping(x.DbContextImplementationType, x.DbContextImplementationType, registeredServiceKey: nameAndLifeStyle.Item1);
				container.RegisterMapping(typeof(DbContext), x.DbContextImplementationType, registeredServiceKey: nameAndLifeStyle.Item1);
			}
		}

		private Tuple<string, IReuse> GetNameAndLifeStyle(uint index, string baseName)
		{
			Contract.Requires(index < 3,
							  "there are only three supported lifestyles; per transaction, per web request and transient");
			Contract.Ensures(Contract.Result<Tuple<string, IReuse>>() != null);

			switch (_defaultDbContextLifeStyle)
			{
				case DefaultLifeStyleOption.PerTransaction:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + DbContextPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + DbContextPWRSuffix, Reuse.InWebRequest);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + DbContextTransientSuffix, Reuse.Transient);
					break;
				case DefaultLifeStyleOption.PerWebRequest:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + DbContextPWRSuffix, Reuse.InWebRequest);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + DbContextPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + DbContextTransientSuffix, Reuse.Transient);
					break;
				case DefaultLifeStyleOption.Transient:
					if (index == 0)
						return new Tuple<string, IReuse>(baseName + DbContextTransientSuffix, Reuse.Transient);
					if (index == 1)
						return new Tuple<string, IReuse>(baseName + DbContextPerTxSuffix, AutoTxReuse.PerTopTransaction);
					if (index == 2)
						return new Tuple<string, IReuse>(baseName + DbContextPWRSuffix, Reuse.InWebRequest);
					break;
				default:
					throw new EFCoreFacilityException("Unknown default life style - please file a bug report");
			}
			throw new EFCoreFacilityException("Invalid index passed to GetNameAndLifeStyle<T> - please file a bug report");
		}

		private class Data
		{
			public IEFCoreInstaller Instance;
			public DbContextOptions Config;
			public Type DbContextImplementationType;
		}
	}
}