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

using System.Runtime.InteropServices;

namespace Castle.Transactions.IO
{
	using System;
	using System.Runtime.ConstrainedExecution;
	using System.Security;

	using Microsoft.Win32.SafeHandles;

	///<summary>
	///	A safe file handle on the transaction resource.
	///</summary>
	[SecurityCritical, ComVisible(false)]
	public sealed class SafeKernelTransactionHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		///<summary>
		///	c'tor taking a pointer to a transaction.
		///</summary>
		///<param name = "handle">The transactional handle.</param>
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public SafeKernelTransactionHandle(IntPtr handle)
			: base(true)
		{
			base.handle = handle;
		}

		protected override bool ReleaseHandle()
		{
			if (!(IsInvalid || IsClosed))
				return Castle.IO.FileSystems.Local.Win32.Interop.NativeMethods.CloseHandle(handle);
			return (IsInvalid || IsClosed);
		}
	}
}