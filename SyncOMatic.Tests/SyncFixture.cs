using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SyncOMatic;

[TestFixture]
public class SyncFixture
{
    [Test]
    [Explicit]
    public void Sync()
    {
        using (var som = new Syncer(Helper.Credentials, Helper.Proxy, DiffFixture.ConsoleLogger))
        {
            PerformRepoSync(som, "NServiceBus.Distributor.Msmq", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.Gateway", "develop", "src", null, null);
            PerformRepoSync(som, "PlatformInstaller", "master", "src", null, null);
            PerformRepoSync(som, "NServiceBus.SqlServer", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.NHibernate", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.RabbitMQ", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.RavenDB", "develop", "src", null, null);
            PerformRepoSync(som, "ServicePulse", "develop", "src", null, null);
            PerformRepoSync(som, "ServiceControl", "develop", "src", null, null);
            PerformRepoSync(som, "Operations.Licensing", "master", "src", null, null);
            PerformRepoSync(som, "ServiceMatrix", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus", "develop", "src", null, null);
            PerformRepoSync(som, "ServiceInsight", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.Azure", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.PowerShell", "develop", "src", null, null);
            PerformRepoSync(som, "NServiceBus.Unity", "develop", "src", null, null);
            
        }
    }

    [Test]
    [Explicit]
    public void SyncOneRepoi()
    {
        using (var som = new Syncer(Helper.Credentials, Helper.Proxy, DiffFixture.ConsoleLogger))
        {
            PerformRepoSync(som, "GitHubReleaseNotes", "master", "src", null, null);
        }
    }

    void PerformRepoSync(Syncer som, string repoName, string defaultBranch, string srcRoot, string solutionName, List<SyncItem> itemsToSync)
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

        var createdSyncBranch = som.Sync(diff, SyncOutput.CreatePullRequest, new[] { "Internal refactoring" }).FirstOrDefault();


        if (string.IsNullOrEmpty(createdSyncBranch))
        {
            Console.Out.WriteLine("Repo {0} is in sync", repoName);
        }
        else
        {
            Console.Out.WriteLine("Pull created for {0}, click here to review and pull: {1}", repoName, createdSyncBranch);
        }
    }
}