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

namespace Castle.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;

	using Castle.IO.Contracts;

	/// <summary>
	/// 	A directory pointer. It might point to an existing directory or 
	/// 	be merely a handle that points to a directory that could be.
	/// </summary>
	[ContractClass(typeof(IDirectoryContract))]
	public interface IDirectory : IFileSystemItem<IDirectory>
	{
		/// <summary>
		/// </summary>
		/// <param name = "directoryName"></param>
		/// <returns></returns>
		IDirectory GetDirectory(string directoryName);

		/// <summary>
		/// </summary>
		/// <param name = "fileName"></param>
		/// <returns></returns>
		IFile GetFile(string fileName);

		/// <summary>
		/// </summary>
		/// <returns></returns>
		IEnumerable<IFile> Files();

		/// <summary>
		/// </summary>
		/// <returns></returns>
		IEnumerable<IDirectory> Directories();

		/// <summary>
		/// </summary>
		/// <param name = "filter"></param>
		/// <param name = "scope"></param>
		/// <returns></returns>
		IEnumerable<IFile> Files(string filter, SearchScope scope);

		/// <summary>
		/// </summary>
		/// <param name = "filter"></param>
		/// <param name = "scope"></param>
		/// <returns></returns>
		IEnumerable<IDirectory> Directories(string filter, SearchScope scope);

		/// <summary>
		/// 	Gets whether this directory pointer is a hard link.
		/// </summary>
		bool IsHardLink { get; }

		/// <summary>
		/// 	TODO: Creates a symlink/hardlink/whatever --- specify this further.
		/// </summary>
		/// <param name = "path"></param>
		/// <returns></returns>
		IDirectory LinkTo(Path path);

		/// <summary>
		/// </summary>
		IDirectory Target { get; }

		// TODO: Move to extension method?
		/// <summary>
		/// </summary>
		/// <param name = "filter"></param>
		/// <param name = "includeSubdirectories"></param>
		/// <param name = "created"></param>
		/// <param name = "modified"></param>
		/// <param name = "deleted"></param>
		/// <param name = "renamed"></param>
		/// <returns></returns>
		IDisposable FileChanges(string filter = "*", bool includeSubdirectories = false, Action<IFile> created = null,
		                        Action<IFile> modified = null, Action<IFile> deleted = null, Action<IFile> renamed = null);
	}
}