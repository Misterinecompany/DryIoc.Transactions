#region license

// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

namespace Castle.Transactions.IO.Tests
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Castle.IO.Extensions;

	using NUnit.Framework;

	using SharpTestsEx;

	public class File_Specs : TxFTestFixtureBase
	{
		private string _TfPath;

		[SetUp]
		public void TFSetup()
		{
			var dllPath = Environment.CurrentDirectory;
			_TfPath = dllPath.CombineAssert("File_Specs");
		}

		[TearDown]
		public void TFTearDown()
		{
			if (_TfPath != null)
				Directory.Delete(_TfPath, true);
		}

		[Test]
		public void Smoke()
		{
		}

		[Test]
		public void WriteAllText()
		{
			var filepath = _TfPath.Combine("write-text.txt");

			using (ITransaction tx = new FileTransaction("Commit TX"))
			{
				var fa = (IFileAdapter) tx;
				fa.WriteAllText(filepath, "Transacted file.");
				tx.Complete();
				Assert.That(tx.State == TransactionState.CommittedOrCompleted);
			}

			File.Exists(filepath).Should("exist after the transaction")
				.Be.True();

			File.ReadAllLines(filepath)
				.First().Should()
				.Be.EqualTo("Transacted file.");
		}

		[Test]
		public void Move_ToDirectory()
		{
			var folder = _TfPath.CombineAssert("source_folder");
			var toFolder = _TfPath.CombineAssert("target_folder");
			const string fileName = "Move_ToDirectory.txt";
			var file = folder.Combine(fileName);

			Directory.Exists(toFolder).Should().Be.False();
			(File.Exists(file)).Should().Be.False();

			File.WriteAllText(file, "this string is the contents of the file");

			using (ITransaction t = new FileTransaction("moving file"))
			{
				File.Exists(toFolder.Combine("file"))
					.Should("not exist before move")
					.Be.False();

				// moving file to folder
				((IFileAdapter) t)
					.Move(file, toFolder);

				File.Exists(file)
					.Should("call through tx and be deleted")
					.Be.False();

				File.Exists(toFolder.Combine("file"))
					.Should("call through tx and visible in its new location")
					.Be.True();

				t.Complete();
			}

			File.Exists(toFolder.Combine("file"))
				.Should(
					"be visible to the outside now and since we tried to move it to " +
					" an existing folder, it should put itself in that folder with its current name.")
				.Be.True();
		}

		[Test]
		public void Move_ToDirectory_PlusFileName()
		{
			// given
			var folder = _TfPath.CombineAssert("source_folder");
			var toFolder = _TfPath.CombineAssert("target_folder");
			const string fileName = "Move_ToDirectory_PlusFileName.txt";
			var file = folder.Combine(fileName);

			File.Exists(file)
				.Should()
				.Be.False();

			File.WriteAllText(file, "testing move");

			// when
			using (ITransaction t = new FileTransaction())
			{
				File.Exists(toFolder.Combine(fileName))
					.Should("not exist before move")
					.Be.False();

				// method under test:
				((IFileAdapter) t).Move(file, toFolder.Combine(fileName));

				File.Exists(file)
					.Should("call through tx and be deleted")
					.Be.False();

				t.Complete();
			}

			// then
			File.Exists(toFolder.Combine(fileName))
				.Should("call through tx and visible in its new location")
				.Be.True();

			File.ReadAllText(toFolder.Combine(fileName))
				.Should("have same contents")
				.Be("testing move");
		}

		[Test]
		public void Write_And_ReplaceContents()
		{
			var filePath = _TfPath.Combine("Write_And_ReplaceContents.txt");

			// simply write something to to file.
			using (var wr = File.CreateText(filePath))
				wr.WriteLine("Hello");

			using (ITransaction tx = new FileTransaction())
			{
				using (var fs = ((IFileAdapter) tx).Create(filePath))
				{
					var str = new UTF8Encoding().GetBytes("Goodbye");
					fs.Write(str, 0, str.Length);
					fs.Flush();
				}

				tx.Complete();
			}

			File.ReadAllLines(filePath)
				.First().Should()
				.Be.EqualTo("Goodbye");
		}

		[Test]
		public void CreateFileTransactionally_ThenRollback()
		{
			var filePath = _TfPath.Combine("transacted-file");

			// simply write something to to file.
			using (var wr = File.CreateText(filePath))
				wr.WriteLine("CreateFileTransactionally_ThenRollback");

			using (ITransaction tx = new FileTransaction("rollback tx"))
			{
				var fa = (IFileAdapter) tx;
				using (var fs = fa.Open(filePath, FileMode.Truncate))
				{
					var str = new UTF8Encoding().GetBytes("Goodbye");
					fs.Write(str, 0, str.Length);
					fs.Flush();
				}

				tx.Rollback();
			}

			File.ReadAllLines(filePath)
				.First().Should().Be.EqualTo("CreateFileTransactionally_ThenRollback");
		}
	}
}