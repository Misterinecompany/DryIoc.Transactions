﻿// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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

using System;
using System.Diagnostics.Contracts;

namespace DryIoc.Transactions.Internal
{
	/// <summary>
	/// Class that simply implements the data-bearing interface <see cref="ICreatedTransaction"/>.
	/// </summary>
	public sealed class CreatedTransaction : ICreatedTransaction
	{
		private readonly ITransaction transaction;
		private readonly bool shouldFork;
		private readonly Func<IDisposable> forkScopeFactory;

		public CreatedTransaction(ITransaction transaction, bool shouldFork, Func<IDisposable> forkScopeFactory)
		{
			Contract.Requires(forkScopeFactory != null);
			Contract.Requires(transaction != null);
			Contract.Requires(transaction.State == TransactionState.Active);

			this.transaction = transaction;
			this.shouldFork = shouldFork;
			this.forkScopeFactory = forkScopeFactory;
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(transaction != null);
			Contract.Invariant(forkScopeFactory != null);
		}

		ITransaction ICreatedTransaction.Transaction
		{
			get { return transaction; }
		}

		bool ICreatedTransaction.ShouldFork
		{
			get { return shouldFork; }
		}

		IDisposable ICreatedTransaction.GetForkScope()
		{
			var disposable = forkScopeFactory();

			if (disposable == null)
				throw new InvalidOperationException("fork scope factory returned null!");

			return disposable;
		}
	}
}