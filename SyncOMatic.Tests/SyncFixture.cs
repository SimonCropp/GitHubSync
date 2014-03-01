namespace SyncOMatic.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class SyncFixture
    {
        [Test]
        [Explicit]
        public void Sync()
        {
            using (var som = new SyncOMatic(Helper.Credentials, Helper.Proxy, DiffFixture.ConsoleLogger))
            {
                PerformRepoSync(som, "NServiceBus.SqlServer", "develop", "src", null, null);
                PerformRepoSync(som, "NServiceBus.NHibernate", "develop", "src", null, null);
                PerformRepoSync(som, "Operations.LicenseGenerator", "master", "src", null, null);
                PerformRepoSync(som, "ServiceMatrix", "develop", "src", null, null);
                PerformRepoSync(som, "NServiceBus", "develop", "src", null, null);
                PerformRepoSync(som, "ServiceInsight", "develop", "src", null, null);
                PerformRepoSync(som, "NServiceBus.Azure", "develop", "src", null, null);
            }
        }

        void PerformRepoSync(SyncOMatic som, string repoName, string defaultBranch, string srcRoot, string solutionName, List<SyncItem> itemsToSync)
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

            var diff = som.Diff(toSync.GetMapper(itemsToSync));
            Assert.NotNull(diff);

            var createdSyncBranch = som.Sync(diff, SyncOutput.CreatePullRequest).FirstOrDefault();


            if (string.IsNullOrEmpty(createdSyncBranch))
            {
                Console.Out.WriteLine("Repo {0} is in sync",repoName);
            }
            else
            {
                Console.Out.WriteLine("Pull created for {0}, click here to review and pull: {1}", repoName, createdSyncBranch);
            }
        }
    }
}
