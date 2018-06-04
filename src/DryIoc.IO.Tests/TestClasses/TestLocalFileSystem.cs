﻿using Castle.IO.FileSystems.Local;

namespace Castle.IO.Tests.TestClasses
{
	public class TestLocalFileSystem : IFileSystem
	{
		public IDirectory GetDirectory(string directoryPath)
		{
			return _local.GetDirectory(directoryPath);
		}

		public IDirectory GetDirectory(Path path)
		{
			return _local.GetDirectory(path);
		}

		public Path GetPath(string path)
		{
			return _local.GetPath(path);
		}

		public ITemporaryDirectory CreateTempDirectory()
		{
			return _local.CreateTempDirectory();
		}

		public IDirectory CreateDirectory(string path)
		{
			return _local.CreateDirectory(path);
		}

		public IDirectory CreateDirectory(Path path)
		{
			return _local.CreateDirectory(path);
		}

		public IFile GetFile(string itemSpec)
		{
			return _local.GetFile(itemSpec);
		}

		public ITemporaryFile CreateTempFile()
		{
			return _local.CreateTempFile();
		}

		public IDirectory GetTempDirectory()
		{
			return _local.GetTempDirectory();
		}

		public IDirectory GetCurrentDirectory()
		{
			return _local.GetCurrentDirectory();
		}

		IFileSystem _local = LocalFileSystem.Instance;
	}
}