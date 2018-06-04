#region license

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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Castle.IO;
using Castle.IO.Internal;
using NLog;

namespace Castle.Transactions.IO
{
	/// <summary>
	/// 	Adapter class for the file transactions
	/// 	which implement the same interface.
	/// 
	/// 	This adapter chooses intelligently whether there's an ambient
	/// 	transaction, and if there is, joins it.
	/// </summary>
	public sealed class FileAdapter : TransactionAdapterBase, IFileAdapter
	{
		internal const int ChunkSize = 4096;
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();

		///<summary>
		///	c'tor
		///</summary>
		public FileAdapter() : this(false, null)
		{
		}

		///<summary>
		///	c'tor
		///</summary>
		///<param name = "constrainToSpecifiedDir"></param>
		///<param name = "specifiedDir"></param>
		public FileAdapter(bool constrainToSpecifiedDir, string specifiedDir) : base(constrainToSpecifiedDir, specifiedDir)
		{
			if (constrainToSpecifiedDir)
				_Logger.Debug(() => string.Format("FileAdapter c'tor, constraining to dir: {0}", specifiedDir));
			else
				_Logger.Debug(() => "FileAdapter c'tor, no directory constraint.");
		}

		///<summary>
		///	Creates a new file from the given path for ReadWrite,
		///	different depending on whether we're in a transaction or not.
		///</summary>
		///<param name = "path">Path to create file at.</param>
		///<returns>A filestream for the path.</returns>
		public FileStream Create(string path)
		{
			AssertAllowed(path);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
				return ((IFileAdapter) mTx.Value).Create(path);
#endif

			return LongPathFile.Open(path, FileMode.Create, FileAccess.ReadWrite);
		}

		///<summary>
		///	Returns whether the specified file exists or not.
		///</summary>
		///<param name = "filePath">The file path.</param>
		///<returns></returns>
		public bool Exists(string filePath)
		{
			AssertAllowed(filePath);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
				return ((IFileAdapter) mTx.Value).Exists(filePath);
#endif
			return LongPathFile.Exists(filePath);
		}

		public string ReadAllText(string path, Encoding encoding)
		{
			AssertAllowed(path);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
				return ((IFileAdapter) mTx.Value).ReadAllText(path, encoding);
#endif

			using (var rdr = new StreamReader(Open(path, FileMode.Open)))
				return rdr.ReadToEnd();
		}

		public void Move(string originalFilePath, string newFilePath)
		{
			AssertAllowed(originalFilePath);
			AssertAllowed(newFilePath);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
			{
				((IFileAdapter) mTx.Value).Move(originalFilePath, newFilePath);
				return;
			}
#endif

			LongPathFile.Move(originalFilePath, newFilePath);
		}

		public IEnumerable<string> ReadAllLines(string filePath)
		{
			return ReadAllLinesEnumerable(filePath);
		}

		public IEnumerable<string> ReadAllLinesEnumerable(string filePath)
		{
			using (var rdr = new StreamReader(Open(filePath, FileMode.Open), true))
			{
				var readLine = rdr.ReadLine();
				while (readLine != null) 
					yield return readLine;
			}
		}

		public FileStream OpenWrite(string path)
		{
			throw new NotImplementedException();
		}

		public StreamWriter CreateText(string filePath)
		{
			return new StreamWriter(Open(filePath, FileMode.CreateNew));
		}

		public string ReadAllText(string path)
		{
			return ReadAllText(path, Encoding.UTF8);
		}

		public void WriteAllText(string targetPath, string contents)
		{
			AssertAllowed(targetPath);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
			{
				((IFileAdapter) mTx.Value).WriteAllText(targetPath, contents);
				return;
			}
#endif

			using (var writer = new StreamWriter(Open(targetPath, FileMode.OpenOrCreate), Encoding.UTF8))
				writer.Write(contents);
		}

		public void Delete(string filePath)
		{
			AssertAllowed(filePath);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
			{
				((IFileAdapter)mTx.Value).Delete(filePath);
				return;
			}
#endif
			
			LongPathFile.Delete(filePath);
		}

		public FileStream Open(string filePath, FileMode mode)
		{
			AssertAllowed(filePath);
#if !MONO
			var mTx = CurrentTransaction();
			if (mTx.HasValue)
				return ((IFileAdapter)mTx.Value).Open(filePath, mode);
#endif

			return LongPathFile.Open(filePath, mode, FileAccess.ReadWrite);
		}

		public int WriteStream(string targetPath, Stream sourceStream)
		{
			var offset = 0;
			using (var fs = Create(targetPath))
			{
				var buf = new byte[ChunkSize];
				int read;
				while ((read = sourceStream.Read(buf, 0, buf.Length)) != 0)
				{
					fs.Write(buf, 0, read);
					offset += read;
				}
			}
			return offset;
		}
	}
}