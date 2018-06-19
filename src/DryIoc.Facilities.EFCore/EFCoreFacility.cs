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
		private DefaultLifeStyleOption defaultLifeStyle;

		/// <summary>
		/// 	Instantiates a new EFCoreFacility with the default options, session per transaction.
		/// </summary>
		public EFCoreFacility() : this(DefaultLifeStyleOption.SessionPerTransaction)
		{
		}

		/// <summary>
		/// 	Instantiates a new EFCoreFacility with a given lifestyle option.
		/// </summary>
		/// <param name = "defaultLifeStyle">The Session flush mode.</param>
		public EFCoreFacility(DefaultLifeStyleOption defaultLifeStyle)
		{
			this.defaultLifeStyle = defaultLifeStyle;
		}

		public void Init(IContainer container)
		{
			// TODO
		}
	}
}