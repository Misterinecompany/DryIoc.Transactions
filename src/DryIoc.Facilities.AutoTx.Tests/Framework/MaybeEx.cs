using System;
using DryIoc.Transactions;
using NUnit.Framework;

namespace DryIoc.Facilities.AutoTx.Tests.Framework
{
	public static class MaybeEx
	{
		public static Maybe<T> ShouldBe<T>(this Maybe<T> m, Func<T, bool> test, string msg)
		{
			Assert.That(test(m.Value), msg);
			return m;
		}

		public static Maybe<T> ShouldPass<T>(this Maybe<T> m, string msg)
		{
			Assert.IsTrue(m.HasValue, msg);
			return m;
		}
	}
}