﻿#region license

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

using System;
using System.Diagnostics.Contracts;
using DryIoc.Transactions;

namespace DryIoc.Facilities.AutoTx.Contracts
{
	[ContractClassFor(typeof (ITransactionMetaInfoStore))]
	internal abstract class TransactionMetaInfoStoreContract : ITransactionMetaInfoStore
	{
		Maybe<TransactionalClassMetaInfo> ITransactionMetaInfoStore.GetMetaFromType(Type implementation)
		{
			Contract.Requires(implementation != null);
			var metaFromType = Contract.Result<Maybe<TransactionalClassMetaInfo>>();
			Contract.Ensures(metaFromType != null,
			                 "the meta-info contract must return a non-null maybe (the maybe wraps null, also, so this should be an easy check)");
			return metaFromType;
		}
	}
}