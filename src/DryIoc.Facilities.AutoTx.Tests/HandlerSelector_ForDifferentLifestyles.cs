using System;
using Castle.Transactions;
using NUnit.Framework;
using Castle.Facilities.AutoTx.Testing;
using DryIoc;
using DryIoc.Facilities.AutoTx.Extensions;

namespace Castle.Facilities.AutoTx.Tests
{
	public class HandlerSelector_ForDifferentLifeStyles
	{
		private IContainer _Container;

		[SetUp]
		public void SetUp()
		{
			_Container = new Container();
			
			_Container.Register<IHaveLifestyle, PerTxClass>(AutoTxReuse.PerTransaction, Parameters.Of.Type(request => "ordinary"), serviceKey: "ordinary");
			_Container.Register<IHaveLifestyle, PerTxClass>(AutoTxReuse.PerTransaction, Parameters.Of.Type(request => "special"), serviceKey: "special");
			_Container.Register<IHaveLifestyle, TransientClass>(Reuse.Transient, serviceKey: "transient");
			
			_Container.Register<IHaveLifestyle>(Made.Of(
				() => DefaultToTransientLifeStyleFactory.CreateHavingLifestyle(Arg.Of<IContainer>(), Arg.Of<ITransactionManager>()),
				request => request.Parent.ImplementationType));

			_Container.AddAutoTx();
		}

		[Test]
		public void ResolveOutsideOfTransaction()
		{
			Assert.That(_Container.Resolve<IHaveLifestyle>().Work(),
			            Is.EqualTo("from transient"));
		}

		[Test, Description("This test verifies that ")]
		public void ResolveInsideTransaction()
		{
			using (var tx = _Container.ResolveScope<ITransactionManager>()
				.Service.CreateTransaction().Value.Transaction)
			{
				Assert.That(_Container.Resolve<IHaveLifestyle>().Work(),
				            Is.EqualTo("from transaction ordinary"));

				tx.Complete();
			}
		}

		[Test, Description("When resolving in transaction PerTxClass should be the resulting concrete type "
			+ "and because we're resolving the 'special' key of it, that should be its c'tor parameter " 
			+ "(something which tests the ordering of IHandler[] in IHandlerSelector).")]
		public void ResolveInsideTransaction_ByName()
		{
			using (var tx = _Container.ResolveScope<ITransactionManager>()
				.Service.CreateTransaction().Value.Transaction)
			{
				Assert.That(_Container.Resolve<IHaveLifestyle>("special").Work(),
				            Is.EqualTo("from transaction special"));

				tx.Complete();
			}
		}
	}

	public class DefaultToTransientLifeStyleFactory
	{
		public static IHaveLifestyle CreateHavingLifestyle(IContainer container, ITransactionManager transactionManager)
		{
			return container.Resolve<IHaveLifestyle>(transactionManager.CurrentTransaction.HasValue ? "ordinary" : "transient");
		}
	}

	/*
	/// <summary>
	/// A class for defaulting transacted handlers to non transacted ones when there are no ambient transactions.
	/// The services which this handler selector acts upon is specified by its generic parameter. This class
	/// can be very useful when unit-testing, for example.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DefaultToTransientLifeStyle<T> : IHandlerSelector 
		where T : class
	{
		private readonly ITransactionManager _TransactionManager;

		public DefaultToTransientLifeStyle(ITransactionManager transactionManager)
		{
			Contract.Requires(transactionManager != null);
			_TransactionManager = transactionManager;
		}

		public bool HasOpinionAbout(string key, Type service)
		{
			return service == typeof (T);
		}

		public IHandler SelectHandler(string key, Type service, IHandler[] handlers)
		{
			return handlers.First(x => _TransactionManager.CurrentTransaction.HasValue
								? x.ComponentModel.LifestyleType == LifestyleType.Custom
									&& typeof(PerTransactionLifestyleManagerBase).IsAssignableFrom(InnerLifeStyle(x))
				            	: x.ComponentModel.LifestyleType == LifestyleType.Transient);
		}

		// select the first generic argument, in our case the transaction lifestyle manager type
		private static Type InnerLifeStyle(IHandler x)
		{
			return x.ComponentModel.CustomLifestyle.IsGenericType 
				? x.ComponentModel.CustomLifestyle.GetGenericArguments()[0] 
				: x.ComponentModel.CustomLifestyle;
		}
	}
	*/

	public interface IHaveLifestyle
	{
		string Work();
	}

	public class PerTxClass : IHaveLifestyle
	{
		private readonly string _Type;

		public PerTxClass(string type)
		{
			if (type == null) throw new ArgumentNullException("type");
			_Type = type;
		}

		[Transaction]
		public string Work()
		{
			return string.Format("from transaction {0}", _Type);
		}
	}

	public class TransientClass : IHaveLifestyle
	{
		public string Work()
		{
			return "from transient";
		}
	}
}