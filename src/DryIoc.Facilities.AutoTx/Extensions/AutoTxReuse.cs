using DryIoc.Facilities.AutoTx.Lifestyles;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class AutoTxReuse
    {
	    public static IReuse PerTransaction => new WrapperPerTransactionReuse();

	    public static IReuse PerTopTransaction => new WrapperPerTopTransactionReuse();
    }
}
