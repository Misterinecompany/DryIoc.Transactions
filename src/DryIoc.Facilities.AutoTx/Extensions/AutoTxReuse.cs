using DryIoc.Facilities.AutoTx.Lifestyles;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class AutoTxReuse
    {
	    public static readonly IReuse PerTransaction = new WrapperPerTransactionReuse();

	    public static readonly IReuse PerTopTransaction = new WrapperPerTopTransactionReuse();
    }
}
