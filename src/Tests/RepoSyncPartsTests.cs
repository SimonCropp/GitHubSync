using GitHubSync;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class RepoSyncPartsTests : TestBase
{
    [Fact]
    public void Simple()
    {
        var repoSync = BuildRepoSync();
        repoSync.AddBlob("added1");
        repoSync.AddBlob("added2","target2");
        repoSync.RemoveBlob("removed1");
        repoSync.RemoveBlob("removed2", "target2");
        repoSync.AddTarget("owner1", "repo1", "branch1");
        repoSync.AddTarget("owner2", "repo2", "branch2");
        Verify(repoSync);
    }
    [Fact]
    public void AddBlob()
    {
        var repoSync = BuildRepoSync();
        repoSync.AddBlob("added1");
        repoSync.AddBlob("added2","target2");
        repoSync.AddBlob("sourceDir/added3","targetDir/target3");
        Verify(repoSync);
    }
    [Fact]
    public void AddTree()
    {
        var repoSync = BuildRepoSync();
        repoSync.AddSourceItem(TreeEntryTargetType.Tree, "added1");
        repoSync.AddSourceItem(TreeEntryTargetType.Tree,"added2","target2");
        repoSync.AddSourceItem(TreeEntryTargetType.Tree,"sourceDir/added3","targetDir/target3");
        Verify(repoSync);
    }

    [Fact]
    public void RemoveBlob()
    {
        var repoSync = BuildRepoSync();
        repoSync.RemoveBlob("added1");
        repoSync.RemoveBlob("added2","target2");
        repoSync.RemoveBlob("sourceDir/added3","targetDir/target3");
        Verify(repoSync);
    }

    [Fact]
    public void AddTarget()
    {
        var repoSync = BuildRepoSync();
        repoSync.AddTarget("repo1");
        repoSync.AddTarget("owner2","repo2","branch2");
        Verify(repoSync);
    }

    static RepoSync BuildRepoSync()
    {
        var credentials = CredentialsHelper.Credentials;
        return new RepoSync(credentials, "owner", "GitHubSync.TestRepository", "source");
    }

    static void Verify(RepoSync repoSync)
    {
        ObjectApprover.VerifyWithJson(
            new
            {
                repoSync.itemsToSync,
                repoSync.targets
            });
    }

    public RepoSyncPartsTests(ITestOutputHelper output) : base(output)
    {
    }
}