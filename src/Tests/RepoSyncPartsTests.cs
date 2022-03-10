using GitHubSync;

[Trait("Category", "Local")]
public class RepoSyncPartsTests :
    XunitContextBase
{
    [Fact]
    public Task Simple()
    {
        var repoSync = BuildRepoSync(SyncMode.IncludeAllByDefault);

        //repoSync.AddBlob("added1");
        //repoSync.AddBlob("added2","target2");
        repoSync.RemoveBlob("removed1");
        repoSync.RemoveBlob("removed2", "target2");

        repoSync.AddTargetRepository(new(null, "owner1", "repo1", "branch1"));
        repoSync.AddTargetRepository(new(null, "owner2", "repo2", "branch2"));

        return Verify(repoSync);
    }

    [Fact]
    public Task AddBlob()
    {
        var repoSync = BuildRepoSync(SyncMode.ExcludeAllByDefault);

        repoSync.AddBlob("added1");
        repoSync.AddBlob("added2", "target2");
        repoSync.AddBlob("sourceDir/added3", "targetDir/target3");

        return Verify(repoSync);
    }

    [Fact]
    public Task AddTree()
    {
        var repoSync = BuildRepoSync(SyncMode.ExcludeAllByDefault);

        repoSync.AddSourceItem(TreeEntryTargetType.Tree, "added1");
        repoSync.AddSourceItem(TreeEntryTargetType.Tree, "added2", "target2");
        repoSync.AddSourceItem(TreeEntryTargetType.Tree, "sourceDir/added3", "targetDir/target3");

        return Verify(repoSync);
    }

    [Fact]
    public Task RemoveBlob()
    {
        var repoSync = BuildRepoSync(SyncMode.IncludeAllByDefault);

        repoSync.RemoveBlob("added1");
        repoSync.RemoveBlob("added2", "target2");
        repoSync.RemoveBlob("sourceDir/added3", "targetDir/target3");

        return Verify(repoSync);
    }

    [Fact]
    public Task AddTarget()
    {
        var repoSync = BuildRepoSync(SyncMode.IncludeAllByDefault);

        repoSync.AddTargetRepository(new(null, "owner1", "repo1", "branch1"));
        repoSync.AddTargetRepository(new(null, "owner2", "repo2", "branch2"));

        return Verify(repoSync);
    }

    static RepoSync BuildRepoSync(SyncMode syncMode)
    {
        var credentials = CredentialsHelper.Credentials;

        var repoSync = new RepoSync(syncMode: syncMode);
        repoSync.AddSourceRepository(new(credentials, "owner", "GitHubSync.TestRepository", "source"));

        return repoSync;
    }

#pragma warning disable CS1998
    // ReSharper disable once UnusedParameter.Local
    static async Task Verify(RepoSync repoSync)
#pragma warning restore CS1998
    {
        // Note: we can't verify against local dummy repositories (yet)
        //foreach (var target in repoSync.targets)
        //{
        //    var syncContext = await repoSync.CalculateSyncContext(target);

        //    await Verify(
        //        new
        //        {
        //            syncContext.Diff,
        //            syncContext.Description,
        //            repoSync.sources
        //        });
        //}
    }

    public RepoSyncPartsTests(ITestOutputHelper output) :
        base(output)
    {
    }
}