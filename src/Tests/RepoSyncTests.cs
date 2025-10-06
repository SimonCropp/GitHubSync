#if DEBUG

public class RepoSyncTests :
    XunitContextBase
{
    [Fact]
    public async Task SyncPrIncludeAllByDefault()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(WriteLine);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(
            $"GitHubSync update - {repoContext.TempBranchName}",
            $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            $"GitHubSync update - {repoContext.TempBranchName}");
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrExcludeAllByDefault()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(WriteLine, syncMode: SyncMode.ExcludeAllByDefault);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddSourceItem(TreeEntryTargetType.Blob,"sourceFile.txt", "nested/sourceFile.txt");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(
            $"GitHubSync update - {repoContext.TempBranchName}",
            $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            $"GitHubSync update - {repoContext.TempBranchName}");
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrMerge()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(WriteLine);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(
            $"GitHubSync update - {repoContext.TempBranchName}",
            $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            $"GitHubSync update - {repoContext.TempBranchName}",
            SyncOutput.MergePullRequest);
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncCommit()
    {
        var credentials = CredentialsHelper.GitHubCredentials;
        var repoSync = new RepoSync(WriteLine);

        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new(credentials, Client.RepositoryOwner, "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(
            $"GitHubSync update - {repoContext.TempBranchName}",
            $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            $"GitHubSync update - {repoContext.TempBranchName}",
            SyncOutput.CreateCommit);
        await repoContext.VerifyCommit(sync.Single());
    }

    public RepoSyncTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
#endif