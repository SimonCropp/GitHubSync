namespace SyncOMatic.Core.Tests
{
    using System;
    using System.Collections.Generic;
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

                foreach (var url in som.Sync(diff, SyncOutput.CreateBranch))
                {
                    Console.WriteLine(url);
                }
            }
        }


    }
}
