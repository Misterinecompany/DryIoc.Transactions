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

namespace Castle.Transactions.IO
{
	using Castle.IO;

	public class TransactionalFileSystem : IFileSystem
	{
		public IDirectory GetDirectory(string directoryPath)
		{
			throw new System.NotImplementedException();
		}

		public IDirectory GetDirectory(Path directoryPath)
		{
			throw new System.NotImplementedException();
		}

		public Path GetPath(string path)
		{
			throw new System.NotImplementedException();
		}

		public ITemporaryDirectory CreateTempDirectory()
		{
			throw new System.NotImplementedException();
		}

		public IDirectory CreateDirectory(string path)
		{
			throw new System.NotImplementedException();
		}

		public IDirectory CreateDirectory(Path path)
		{
			throw new System.NotImplementedException();
		}

		public IFile GetFile(string itemSpec)
		{
			throw new System.NotImplementedException();
		}

		public ITemporaryFile CreateTempFile()
		{
			throw new System.NotImplementedException();
		}

		public IDirectory GetTempDirectory()
		{
			throw new System.NotImplementedException();
		}

		public IDirectory GetCurrentDirectory()
		{
			throw new System.NotImplementedException();
		}
	}
}