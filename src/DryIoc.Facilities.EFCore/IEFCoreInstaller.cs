using System;
using DryIoc.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	public interface IEFCoreInstaller
	{
		/// <summary>
		/// 	Is this the default session factory
		/// </summary>
		bool IsDefault { get; }

		/// <summary>
		/// 	Gets a session factory key. This key must be unique for the registered
		/// 	NHibernate installers.
		/// </summary>
		string SessionFactoryKey { get; }

		///// <summary>
		///// 	An interceptor to assign to the ISession being resolved through this session factory.
		///// </summary>
		//Maybe<IInterceptor> Interceptor { get; }

		// TODO add doc-comment here
		Type DbContextImplementationType { get; }

		/// <summary>
		/// Returns Entity Framework Core configuration
		/// </summary>
		DbContextOptionsBuilder Config { get; } //TODO but internally work with DbContextOptions (Builder is only wrapper on DbContextOptions)

		///// <summary>
		///// 	Call-back to the installer, when the factory is registered
		///// 	and correctly set up in Windsor..
		///// </summary>
		///// <param name = "factory"></param>
		//void Registered(ISessionFactory factory);
	}
}
