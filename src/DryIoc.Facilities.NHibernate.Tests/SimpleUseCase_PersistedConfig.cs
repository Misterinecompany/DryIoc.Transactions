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

using System.IO;
using DryIoc.Facilities.AutoTx.Extensions;
using DryIoc.Facilities.NHibernate.Tests.Extensions;
using DryIoc.Facilities.NHibernate.Tests.Framework;
using DryIoc.Facilities.NHibernate.Tests.TestClasses;
using NUnit.Framework;

namespace DryIoc.Facilities.NHibernate.Tests
{
	public class SimpleUseCase_PersistedConfig : EnsureSchema
	{
		[SetUp]
		public void SetUp()
		{
			if (File.Exists(PersistingInstaller.SerializedConfigFile))
				File.Delete(PersistingInstaller.SerializedConfigFile);

		}

		[Test]
		public void CacheFile_Is_Created()
		{
			using (var c = new Container())
			{
				c.Register<INHibernateInstaller, PersistingInstaller>(Reuse.Singleton);

				c.AddNLogLogging();
				c.AddAutoTx();
				c.AddNHibernate();

				Assert.That(File.Exists(PersistingInstaller.SerializedConfigFile), "Could not find serialized cache file");
			}
		}
	}
}