using System;
using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	public interface IEFCoreInstaller
	{
		/// <summary>
		/// 	Is this the default DbContext factory
		/// </summary>
		bool IsDefault { get; }

		/// <summary>
		/// 	Gets a DbContext factory key. This key must be unique for the registered
		/// 	EFCore installers.
		/// </summary>
		string DbContextFactoryKey { get; }

		/// <summary>
		///		Class which inherits from <see cref="DbContext"/> used as DbContext.
		/// </summary>
		Type DbContextImplementationType { get; }

		/// <summary>
		///		It's possible to register typed DbContextManager to IoC. This type then will return correctly casted DbContext.
		///		This type should inherit from <see cref="DbContextManager{TDbContext}"/>
		/// </summary>
		Type TypedDbContextManagerType { get; }

		/// <summary>
		/// Returns Entity Framework Core configuration
		/// </summary>
		DbContextOptionsBuilder Config { get; }

		/// <summary>
		/// Action performed after transaction commit for DbContext obtained from DbContextManager
		/// </summary>
		TransactionCommitAction TransactionCommitAction { get; }

		/// <summary>
		/// 	Call-back to the installer, when the factory is registered
		/// 	and correctly set up in Container
		/// </summary>
		void Registered();
	}

	public enum TransactionCommitAction
	{
		Nothing,
		Dispose,
	}
}
