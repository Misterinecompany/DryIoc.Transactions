﻿#region license

// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Castle.IO.Tests.TestClasses;
using NUnit.Framework;

namespace Castle.IO.Tests.paths
{
	[TestFixture(typeof (TestInMemoryFileSystem))]
	[TestFixture(typeof (TestLocalFileSystem))]
	public class standard_file_names<T> : file_system_ctxt<T> where T : IFileSystem, new()
	{
		public standard_file_names()
		{
			file = FileSystem.GetTempDirectory().GetFile("filename.first.txt");
		}

		[Test]
		public void name_contains_extension()
		{
			file.Name.ShouldBe("filename.first.txt");
		}

		[Test]
		public void name_without_extension_doesnt_contain_extension()
		{
			file.NameWithoutExtension.ShouldBe("filename.first");
		}

		[Test]
		public void extension_is_correct()
		{
			file.Extension.ShouldBe(".txt");
		}

		private readonly IFile file;
	}
}