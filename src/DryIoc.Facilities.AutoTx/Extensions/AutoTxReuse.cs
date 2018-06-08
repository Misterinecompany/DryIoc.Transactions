using DryIoc.Facilities.AutoTx.Lifestyles;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class AutoTxReuse
    {
	    public static readonly IReuse PerTransaction = new WrapperReuse<PerTransactionReuse>();
    }
}
