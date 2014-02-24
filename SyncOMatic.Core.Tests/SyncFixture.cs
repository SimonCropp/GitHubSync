namespace SyncOMatic.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class SyncFixture
    {
        [TestCase("ServiceMatrix","develop","src",null,null)]
        [TestCase("NServiceBus", "develop", "src", null, null)]
        [TestCase("ServiceInsight", "develop", "src", null, null)]
        public void PerformRepoSync(string repoName, string defaultBranch, string srcRoot, string solutionName, List<SyncItem> itemsToSync)
        {
            if (itemsToSync == null)
            {
                itemsToSync = DefaultTemplateRepo.ItemsToSync;
            }

            if (solutionName == null)
            {
                solutionName = repoName;
            }

            var toSync = new RepoToSync
            {
                Name = repoName,
                Branch = defaultBranch,
                SolutionName = solutionName,
                SrcRoot = srcRoot
            };



            using (var som = new SyncOMatic(Helper.Credentials, Helper.Proxy, DiffFixture.ConsoleLogger))
            {
                var diff = som.Diff(toSync.GetMapper(itemsToSync));
                Assert.NotNull(diff);

                var createdSyncBranch = som.Sync(diff, SyncOutput.CreateBranch).FirstOrDefault();


                if (string.IsNullOrEmpty(createdSyncBranch))
                {
                    Console.Out.WriteLine("Repo {0} is in sync",repoName);
                }
                else
                {
                    Console.Out.WriteLine("Sync branch created for {0}, please click here to create a pull: {1}", repoName, createdSyncBranch);
                }
            }
        }


    }
}
