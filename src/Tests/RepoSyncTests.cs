using System.Linq;
using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Local")]
public class RepoSyncTests :
    XunitApprovalBase
{
    [Fact]
    public async Task SyncPrIncludeAllByDefault()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);
        var repoContext = await TempRepoContext.Create(Context.MethodName);
        {
            repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
            repoSync.RemoveBlob("README.md");
            repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

            var sync = await repoSync.Sync();
            await repoContext.VerifyPullRequest(sync.Single());
        }
    }
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncPrExcludeAllByDefault()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine, syncMode:SyncMode.ExcludeAllByDefault);
        var repoContext = await TempRepoContext.Create(Context.MethodName);
        {
            repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
            repoSync.AddBlob("sourceFile.txt");
            repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", repoContext.TempBranchName));

            var sync = await repoSync.Sync();
            await repoContext.VerifyPullRequest(sync.Single());
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public Task SyncPrMerge()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);

        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        //repoSync.AddBlob("sourceFile.txt");
        repoSync.RemoveBlob("IDoNotExist/MeNeither.txt");
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "targetForMerge"));

        return repoSync.Sync(SyncOutput.MergePullRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public Task SyncCommit()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);

        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        //repoSync.AddBlob("sourceFile.txt");
        repoSync.RemoveBlob("IDoNotExist/MeNeither.txt");
        repoSync.RemoveBlob("README.md");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "targetForCommit"));

        return repoSync.Sync(SyncOutput.CreateCommit);
    }

    public RepoSyncTests(ITestOutputHelper output) :
        base(output)
    {
    }
}