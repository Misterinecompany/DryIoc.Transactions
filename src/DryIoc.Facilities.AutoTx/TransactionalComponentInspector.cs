#region license

// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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

#endregion

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using DryIoc.Facilities.AutoTx.Errors;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.AutoTx.Utils;
using DryIoc.Transactions;

namespace DryIoc.Facilities.AutoTx
{
	/// <summary>
	/// 	Transaction component inspector that selects the methods
	/// 	available to get intercepted with transactions.
	/// </summary>
	internal class TransactionalComponentInspector
	{
		private readonly IContainer _Container;
		private readonly ITransactionMetaInfoStore _MetaStore;
		private readonly ProxyTypeStorage _ProxyTypeStorage;

		public TransactionalComponentInspector(IContainer container)
		{
			_Container = container;
			_MetaStore = container.Resolve<ITransactionMetaInfoStore>();
			_ProxyTypeStorage = container.Resolve<ProxyTypeStorage>();
		}

		public void ProcessModel(ServiceRegistrationInfo model)
		{
			if (model.Factory.ImplementationType == null) // some registrations don't have implementation type (e.g. ILogger)
				return;
			
			Contract.Assume(model.Factory.ImplementationType != null);

			Validate(model);
			AddInterceptor(model);
		}

		private void Validate(ServiceRegistrationInfo model)
		{
			Contract.Requires(model.Factory.ImplementationType != null);
			Contract.Ensures(model.Factory.ImplementationType != null);

			Maybe<TransactionalClassMetaInfo> meta;
			List<string> problematicMethods;
			if (model.ServiceType == null
			    || model.ServiceType.IsInterface
			    || !(meta = _MetaStore.GetMetaFromType(model.Factory.ImplementationType)).HasValue
			    || (problematicMethods = (from method in meta.Value.TransactionalMethods
			                              where !method.IsVirtual
			                              select method.Name).ToList()).Count == 0)
				return;

			throw new AutoTxFacilityException(string.Format("The class {0} wants to use transaction interception, " +
			                                          "however the methods must be marked as virtual in order to do so. Please correct " +
			                                          "the following methods: {1}", model.Factory.ImplementationType.FullName,
			                                          string.Join(", ", problematicMethods.ToArray())));
		}

		private void AddInterceptor(ServiceRegistrationInfo model)
		{
			Contract.Requires(model.Factory.ImplementationType != null);
			var meta = _MetaStore.GetMetaFromType(model.Factory.ImplementationType);

			if (!meta.HasValue)
				return;

			var proxyType = _Container.CreateProxy(model.ServiceType);
			if (_ProxyTypeStorage.TryAddMapping(proxyType, model.Factory.ImplementationType))
			{
				_Container.Intercept<TransactionInterceptor>(model.ServiceType, proxyType);
			}
		}
	}
}