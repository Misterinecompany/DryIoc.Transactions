using DryIoc.Facilities.AutoTx.Abstraction;

namespace DryIoc.Facilities.AutoTx.Extensions
{
    public static class DryIocExtensions
    {
		public static void AddFacility<T>(this IContainer container) where T : IFacility, new()
		{
			var facility = new T();
			facility.Init(container);
	    }

	    public static void Release(this IContainer container, object instance)
	    {
			// TODO probably not required
	    }
	}
}
