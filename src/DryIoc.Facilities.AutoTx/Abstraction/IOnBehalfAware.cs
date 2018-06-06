namespace DryIoc.Facilities.AutoTx.Abstraction
{
    public interface IOnBehalfAware
    {
	    void SetInterceptedComponentModel(ServiceRegistrationInfo target);
    }
}
