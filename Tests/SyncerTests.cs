using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

public class SyncerTests:TestBase
{
    [Fact]
    public async Task Sync()
    {
        var syncItems = new List<SyncItem>
        {
            new SyncItem
            {
                Parts = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "source", "sourceFile.txt")
            }
        };
        var toSync = new RepoToSync
        {
            Org = "SimonCropp",
            Repo = "GitHubSync.TestRepository",
            TargetBranch = "target"
        };

        using (var som = new Syncer(CredentialsHelper.Credentials, null, WriteLog))
        {
            var diff = await som.Diff(toSync.GetMapper(syncItems));
            var sync = await som.Sync(diff, SyncOutput.CreatePullRequest, new[] { "Internal refactoring" });
            var createdSyncBranch = sync.FirstOrDefault();

            if (string.IsNullOrEmpty(createdSyncBranch))
            {
                Console.Out.WriteLine("Repo {0} is in sync", toSync);
            }
            else
            {
                Console.Out.WriteLine("Pull created for {0}, click here to review and pull: {1}", toSync, createdSyncBranch);
            }
        }
    }

    public SyncerTests(ITestOutputHelper output) : base(output)
    {
    }
}