using DryIoc.Facilities.EFCore.Errors;

namespace DryIoc.Facilities.EFCore
{
	public static class DryIocEFCoreExtensions
	{
		public static void AssertHasFacility<T>(this IContainer container)
		{
			var type = typeof(T);
			if (!container.IsRegistered(type))
				throw new EFCoreFacilityException(
					$"The EFCoreFacility is dependent on the '{type}' facility. " +
					$"Please add the \"{type.Name}\" facility to container.");
		}

		public static void AddEFCore(this IContainer container)
		{
			var efCoreFacility = new EFCoreFacility();
			efCoreFacility.Init(container);
		}

		public static void AddEFCore(this IContainer container, DefaultLifeStyleOption defaultLifeStyle)
		{
			var efCoreFacility = new EFCoreFacility(defaultLifeStyle);
			efCoreFacility.Init(container);
		}
	}
}