﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Configuration;
using System.Diagnostics.Contracts;
using DryIoc.Transactions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.AdoNet;

namespace DryIoc.Facilities.NHibernate.Tests.TestClasses
{
	internal class ExampleInstaller : INHibernateInstaller
	{
		public const string Key = "sf.default";
		private readonly Maybe<IInterceptor> interceptor;

		public ExampleInstaller()
		{
			interceptor = Maybe.None<IInterceptor>();
		}

		public ExampleInstaller(IInterceptor interceptor)
		{
			this.interceptor = Maybe.Some(interceptor);
		}

		public Maybe<IInterceptor> Interceptor
		{
			get { return interceptor; }
		}

		public global::NHibernate.Cfg.Configuration Config
		{
			get { return BuildFluent().BuildConfiguration(); }
		}

		public bool IsDefault
		{
			get { return true; }
		}

		public string SessionFactoryKey
		{
			get { return Key; }
		}

		private FluentConfiguration BuildFluent()
		{
			var connectionString = AppConfig.TestConnectionString;
			Contract.Assume(connectionString != null, "please set the \"test\" connection string in app.config");

			return Fluently.Configure()
				.Database(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString))
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<ThingMap>());
		}

		public void Registered(ISessionFactory factory)
		{
		}

		public virtual global::NHibernate.Cfg.Configuration Deserialize()
		{
			return null;
		}

		public virtual void Serialize(global::NHibernate.Cfg.Configuration configuration)
		{
		}

		public virtual void AfterDeserialize(global::NHibernate.Cfg.Configuration configuration)
		{
		}
	}
}