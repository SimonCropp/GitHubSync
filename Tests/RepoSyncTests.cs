using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

public class RepoSyncTests : TestBase
{
    [Fact]
    public Task Sync()
    {
        var repoSync = new RepoSync(CredentialsHelper.Credentials, "SimonCropp", "GitHubSync.TestRepository","source", WriteLog);
        repoSync.AddSourceItem(TreeEntryTargetType.Blob, "sourceFile.txt");
        repoSync.AddTarget("SimonCropp", "GitHubSync.TestRepository", "target");

        return repoSync.Sync();
    }

    public RepoSyncTests(ITestOutputHelper output) : base(output)
    {
    }
}