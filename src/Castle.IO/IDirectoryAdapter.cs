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

namespace Castle.IO
{
	using System;
	using System.Diagnostics.Contracts;

	using Castle.IO.Contracts;

	///<summary>
	///	Directory helper. Use this instead of Directory in order to gain
	///	transparent interop with transactions (when you want them, as marked by the [Transaction] attribute).
	///</summary>
	[ContractClass(typeof(IDirectoryAdapterContract))]
	public interface IDirectoryAdapter
	{
		///<summary>
		///	Creates a directory at the path given.
		///	Contrary to the Win32 API, doesn't throw if the directory already
		///	exists, but instead returns true. The 'safe' value to get returned 
		///	for be interopable with other path/dirutil implementations would
		///	hence be false (i.e. that the directory didn't already exist).
		///</summary>
		///<param name = "path">The path to create the directory at.</param>
		///<return>
		///	True if the directory already existed, False otherwise.
		///</return>
		bool Create(string path);

		/// <summary>
		/// 	Checks whether the path exists.
		/// </summary>
		/// <param name = "path">Path to check.</param>
		/// <returns>True if it exists, false otherwise.</returns>
		bool Exists(string path);

		/// <summary>
		/// 	Deletes a folder recursively.
		/// </summary>
		/// <param name = "path"></param>
		void Delete(string path);

		/// <summary>
		/// 	Deletes an empty directory. Non-empty directories will cause false
		/// 	to be returned.
		/// </summary>
		/// <param name = "path">The path to the folder to delete.</param>
		/// <param name = "recursively">
		/// 	Whether to delete recursively or not.
		/// 	When recursive, we delete all subfolders and files in the given
		/// 	directory as well. If not recursive sub-directories and files will not
		/// 	be deleted.
		/// </param>
		/// <returns>Whether the delete was successful (i.e. the directory existed and was deleted).</returns>
		/// <exception cref = "ArgumentNullException">
		/// 	<paramref name = "path" /> is <see langword = "null" />.
		/// </exception>
		/// <exception cref = "ArgumentException">
		/// 	<paramref name = "path" /> is an empty string (""), contains only white 
		/// 	space, or contains one or more invalid characters as defined in 
		/// 	<see cref = "Path.GetInvalidPathChars" />.
		/// 	<para>
		/// 		-or-
		/// 	</para>
		/// 	<paramref name = "path" /> contains one or more components that exceed
		/// 	the drive-defined maximum length. For example, on Windows-based 
		/// 	platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref = "System.IO.PathTooLongException">
		/// 	<paramref name = "path" /> exceeds the system-defined maximum length. 
		/// 	For example, on Windows-based platforms, paths must not exceed 
		/// 	32,000 characters.
		/// </exception>
		/// <exception cref = "System.IO.DirectoryNotFoundException">
		/// 	<paramref name = "path" /> could not be found.
		/// </exception>
		/// <exception cref = "UnauthorizedAccessException">
		/// 	The caller does not have the required access permissions.
		/// 	<para>
		/// 		-or-
		/// 	</para>
		/// 	<paramref name = "path" /> refers to a directory that is read-only.
		/// </exception>
		/// <exception cref = "System.IO.IOException">
		/// 	<paramref name = "path" /> is a file.
		/// 	<para>
		/// 		-or-
		/// 	</para>
		/// 	<paramref name = "path" /> refers to a directory that is not empty and recursively=true wasn't passed.
		/// 	<para>
		/// 		-or-    
		/// 	</para>
		/// 	<paramref name = "path" /> refers to a directory that is in use.
		/// 	<para>
		/// 		-or-
		/// 	</para>
		/// 	<paramref name = "path" /> specifies a device that is not ready.
		/// </exception>
		bool Delete(string path, bool recursively);

		/// <summary>
		/// 	Gets the full path of the specified directory.
		/// </summary>
		/// <param name = "relativePath">The relative path.</param>
		/// <returns>A string with the full path.</returns>
		string GetFullPath(string relativePath);

		///<summary>
		///	Gets the MapPath of the path. 
		/// 
		///	This will be relative to the root web directory if we're in a 
		///	web site and otherwise to the executing assembly.
		///</summary>
		///<param name = "path"></param>
		///<returns></returns>
		string MapPath(string path);

		///<summary>
		///	Moves the directory from the original path to the new path.
		///</summary>
		///<param name = "originalPath">Path from</param>
		///<param name = "newPath">Path to</param>
		void Move(string originalPath, string newPath);

		/// <summary>
		/// 	<see cref = "Move(string,string)" />. overwrite should be true if you wish to overwrite the target if it exists.
		/// </summary>
		void Move(string originalPath, string newPath, bool overwrite);
	}
}