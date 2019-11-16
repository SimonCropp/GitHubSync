using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Integration")]
public class CleanupTests :
    XunitApprovalBase
{
    [Fact]
    public async Task Run()
    {
        var existing = await Client.GitHubClient.Repository.Branch.GetAll("SimonCropp", "GitHubSync.TestRepository");
        foreach (var branch in existing.Where(x => x.Name.StartsWith("GitHubSync-")))
        {
            await Client.DeleteBranch(branch.Name);
        }
    }

    public CleanupTests(ITestOutputHelper output) :
        base(output)
    {
    }
}