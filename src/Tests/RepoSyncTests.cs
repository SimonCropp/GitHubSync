using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Integration")]
public class RepoSyncTests :
    XunitLoggingBase
{
    [Fact]
    public Task SyncPr()
    {
        var credentials = CredentialsHelper.Credentials;
        var repoSync = new RepoSync(WriteLine);

        repoSync.AddSourceRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "source"));
        //repoSync.AddBlob("sourceFile.txt");
        repoSync.RemoveBlob("IDoNotExist/MeNeither.txt");
        repoSync.RemoveBlob("a/b/c/file.txt");
        repoSync.RemoveBlob("a/b/file.txt");
        repoSync.AddTargetRepository(new RepositoryInfo(credentials, "SimonCropp", "GitHubSync.TestRepository", "target"));

        return repoSync.Sync();
    }

    [Fact]
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