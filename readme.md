# DryIoc Transactions (port from Castle.Transactions https://github.com/castleproject/Castle.Transactions)

A project for transaction management on .NET Standard.

## Quick Start

NuGet package is currently not exist.

### Castle Transactions

The original project that manages transactions.

#### Main Features

 * Regular Transactions (+`System.Transactions` interop) - allows you to create transactions with a nice API
 * Dependent Transactions - allows you to fork dependent transactions automatically by declarative programming: `[Transaction(Fork=true)]`
 * Transaction Logging - A trace listener in namespace `Castle.Transactions.Logging`, named `TraceListener`.
 * Retry policies for transactions

#### Main Interfaces

 - `ITransactionManager`:
   - *default implementation is `TransactionManager`*
   - keeps tabs on what transaction is currently active
   - coordinates parallel dependent transactions
   - keep the light weight transaction manager (LTM) happy on the CLR

### Castle Transactions IO

A project for adding a transactional file system to the mix!

#### Main Features

 * Provides an `Castle.IO.IFileSystem` implementation that adds transactionality to common operations.

