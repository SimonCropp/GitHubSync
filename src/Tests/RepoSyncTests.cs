using System.Linq;
using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Local")]
public class RepoSyncTests :
    XunitContextBase
{
    [Fact]
    public async Task SyncPrIncludeAllByDefault()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync();
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrExcludeAllByDefault()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine, syncMode: SyncMode.ExcludeAllByDefault);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddSourceItem(TreeEntryTargetType.Blob,"sourceFile.txt", "nested/sourceFile.txt");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync();
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncPrMerge()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);
        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(SyncOutput.MergePullRequest);
        await repoContext.VerifyPullRequest(sync.Single());
    }

    [Fact]
    public async Task SyncCommit()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);

        await using var repoContext = await TempRepoContext.Create(Context.MethodName, this);
        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

        var sync = await repoSync.Sync(SyncOutput.CreateCommit);
        await repoContext.VerifyCommit(sync.Single());
    }

    public RepoSyncTests(ITestOutputHelper output) :
        base(output)
    {
    }
}