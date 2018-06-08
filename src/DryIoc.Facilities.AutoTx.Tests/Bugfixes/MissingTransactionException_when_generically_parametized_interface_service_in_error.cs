﻿using System;
using DryIoc;
using DryIoc.Facilities.AutoTx.Extensions;
using NUnit.Framework;
using SharpTestsEx;

namespace Castle.Facilities.AutoTx.Tests.Bugfixes
{
	using Castle.Transactions;

	public class MissingTransactionException_when_generically_parametized_interface_service_in_error
	{
		public interface IUnitOfWork : IDisposable
		{
			object Get(object id);
		}

		internal class UnitOfWorkImpl : IUnitOfWork
		{
			private bool _Disposed;

			public object Get(object id)
			{
				if (_Disposed)
					Assert.Fail("the unit of work is disposed");

				return new User("Hello Pär");
			}

			public void Dispose()
			{
				_Disposed = true;
			}
		}

		internal interface IRepo
		{
			User Load(Guid id);
		}

		public class User
		{
			public string Name { get; set; }

			public User(string name)
			{
				Name = name;
			}
		}

		public class Repo : IRepo
		{
			private readonly Func<IUnitOfWork> _FacUoW;

			public Repo(Func<IUnitOfWork> facUoW)
			{
				_FacUoW = facUoW;
			}

			/// <summary>
			/// Unit of work should not be resolved till this method is called!
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			[Transaction]
			public User Load(Guid id)
			{
				if (System.Transactions.Transaction.Current == null)
					Assert.Fail("we must have a tx active");

				using (var uow = _FacUoW())
					return (User)uow.Get(id);
			}
		}

		internal class MyMessage
		{
			public MyMessage(Guid userId)
			{
				UserId = userId;
			}

			public Guid UserId { get; private set; }
		}

		internal interface IMessageHandler<in T>
		{
			void Handle(T message);
		}

		internal class ServiceClass : IMessageHandler<MyMessage>
		{
			private readonly IRepo _Repo;
			private readonly IBus _Bus;

			public ServiceClass(IRepo repo, IBus bus)
			{
				_Repo = repo;
				_Bus = bus;
			}

			public void Handle(MyMessage message)
			{
				var user = _Repo.Load(message.UserId);
				_Bus.Send(new OtherMessage(user.Name));
			}
		}

		internal interface IBus
		{
			void Send(object o);
		}

		internal class BusImpl : IBus
		{
			public object Sent;

			public void Send(object o)
			{
				Sent = o;
			}
		}

		internal class OtherMessage
		{
			public string Name { get; private set; }

			public OtherMessage(string name)
			{
				Name = name;
			}
		}

		[Test, Description("phew, that's quite a list of classes and interfaces needed!")]
		public void Repro()
		{
			var cont = SetUpContainer();

			var mh = cont.Resolve<IMessageHandler<MyMessage>>();
			mh.Handle(new MyMessage(Guid.NewGuid()));

			var bus = (BusImpl)cont.Resolve<IBus>();
			((OtherMessage) bus.Sent).Name.Should().Be("Hello Pär");
		}

		private IContainer SetUpContainer()
		{
			var container = new Container();

			container.Register<IUnitOfWork, UnitOfWorkImpl>(AutoTxReuse.PerTransaction);
			container.Register<IRepo, Repo>(Reuse.Transient);
			container.Register<IMessageHandler<MyMessage>, ServiceClass>(Reuse.Singleton);
			
			container.Register<IBus, BusImpl>(Reuse.Singleton);

			container.AddFacility<AutoTxFacility>();

			return container;
		}
	}

}