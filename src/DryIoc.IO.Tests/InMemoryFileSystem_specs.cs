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

namespace Castle.IO.Tests
{
	using System.IO;
	using System.Linq;
	using System.Text;

	using Castle.IO.FileSystems.InMemory;
	using Castle.IO.Tests.contexts;

	using NUnit.Framework;

	public class in_mem_specification : in_memory_file_system
	{
		[Test]
		public void folders_and_paths_are_case_insensitive_by_default()
		{
			given_directory(@"C:\test");
			given_directory(@"C:\TEST\test2");

			SpecExtensions.ShouldBeTheSameInstanceAs(FileSystem.GetDirectory(@"C:\TEST"), FileSystem.GetDirectory(@"C:\test"));
		}

		[Test]
		public void paths_are_correct()
		{
			var path = @"C:\tmp\TestPackage-1.0.0.1234.wrap";
			var file = FileSystem.GetFile(path);
			SpecExtensions.ShouldBe<string>(file.ToString(), path);
		}

		[Test]
		public void can_add_folders_to_fs()
		{
			var fs = new InMemoryFileSystem().CreateChildDir(@"C:\mordor");
			fs.Directories.ShouldHaveCountOf(1);
		}

		[Test]
		public void can_add_sub_folders()
		{
			var fs = new InMemoryFileSystem().CreateChildDir(@"C:\mordor\nurn");
			var mordor = fs.GetDirectory(@"C:\mordor");
			mordor.Exists.ShouldBeTrue();

			var nurn = mordor.GetDirectory("nurn");
			nurn.Path.FullPath.ShouldBe(@"C:\mordor\nurn\");
			nurn.Exists.ShouldBeTrue();
		}

		[Test]
		public void can_move_folder()
		{
			var fs = new InMemoryFileSystem();
			var source = fs.GetDirectory("C:\\source");
			source.GetFile("mordor.txt").MustExist();
			var destination = fs.GetDirectory("C:\\destination");
			source.MoveTo(destination);

			source.GetFile("mordor.txt").Exists.ShouldBeFalse();
			source.Exists.ShouldBeFalse();

			destination.Exists.ShouldBeTrue();
			destination.GetFile("mordor.txt").Exists.ShouldBeTrue();
		}

		[Test]
		public void content_is_written_correctly()
		{
			var fs = new InMemoryFileSystem();

			using (var file = fs.CreateTempFile())
			{
				var content = string.Join(" ", Enumerable.Repeat("Test value", 5000).ToArray());
				using (var str = file.OpenWrite())
				using (var sw = new StreamWriter(str, Encoding.UTF8))
				{
					sw.Write(content);
				}
				using (var str = file.OpenRead())
				using (var sr = new StreamReader(str, Encoding.UTF8))
				{
					sr.ReadToEnd().ShouldBe(content);
				}
			}
		}
	}

	namespace contexts
	{
		public abstract class in_memory_file_system : context
		{
			public in_memory_file_system()
			{
				FileSystem = new InMemoryFileSystem();
			}

			protected InMemoryFileSystem FileSystem { get; set; }

			protected void given_directory(string cTest)
			{
				FileSystem.GetDirectory(cTest).MustExist();
			}
		}
	}
}