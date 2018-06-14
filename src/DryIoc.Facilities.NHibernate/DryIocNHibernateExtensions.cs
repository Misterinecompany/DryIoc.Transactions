using DryIoc.Facilities.NHibernate.Errors;

namespace DryIoc.Facilities.NHibernate
{
	public static class DryIocNHibernateExtensions
	{
		public static void AssertHasFacility<T>(this IContainer container)
		{
			var type = typeof(T);
			if (!container.IsRegistered(type))
				throw new NHibernateFacilityException(
					$"The NHibernateFacility is dependent on the '{type}' facility. " +
					$"Please add the \"{type.Name}\" facility to container.");
		}

		public static void AddNHibernate(this IContainer container)
		{
			var nhibernateFacility = new NHibernateFacility();
			nhibernateFacility.Init(container);
		}
	}
}