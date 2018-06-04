﻿#region license

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

namespace Castle.IO.FileSystems
{
	using System.Diagnostics.Contracts;

	public abstract class AbstractDirectory
	{
		protected string NormalizeDirectoryPath(string directoryPath)
		{
			Contract.Requires(directoryPath != null);
			Contract.Ensures(Contract.Result<string>() != null);

			if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString())
			    && !directoryPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
				return directoryPath + Path.DirectorySeparatorChar;

			return directoryPath;
		}
	}
}