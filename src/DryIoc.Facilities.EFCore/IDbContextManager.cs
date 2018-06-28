// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

using Microsoft.EntityFrameworkCore;

namespace DryIoc.Facilities.EFCore
{
	/// <summary>
	/// 	DbContext manager interface. The default
	/// 	DbContext lifestyle is per-transaction, so call OpenDbContext within a transaction!
	/// </summary>
	public interface IDbContextManager
	{
		/// <summary>
		/// 	Gets a new or existing DbContext depending on your context.
		/// </summary>
		/// <returns>A non-null DbContext.</returns>
		DbContext OpenDbContext();

		/// <summary>
		///		Helper method for getting DbContext with specific type. Gets a new or existing DbContext depending on your context.
		/// </summary>
		/// <typeparam name="TDbContext"></typeparam>
		/// <returns>A non-null DbContext.</returns>
		TDbContext OpenDbContextTyped<TDbContext>() where TDbContext : DbContext;
	}
}