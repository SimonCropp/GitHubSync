#if DEBUG

public class RepoSyncTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SyncPrIncludeAllByDefault()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(output.WriteLine);
        await using var repoContext = await TempRepoContext.Create();
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync();
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrExcludeAllByDefault()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(output.WriteLine, syncMode: SyncMode.ExcludeAllByDefault);
        await using var repoContext = await TempRepoContext.Create();
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddSourceItem(TreeEntryTargetType.Blob,"sourceFile.txt", "nested/sourceFile.txt");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync();
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrMerge()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(output.WriteLine);
        await using var repoContext = await TempRepoContext.Create();
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(SyncOutput.MergePullRequest);
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncCommit()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(output.WriteLine);

        await using var repoContext = await TempRepoContext.Create();
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(SyncOutput.CreateCommit);
        await repoContext.VerifyCommit(sync.Single());
    }
}
#endif