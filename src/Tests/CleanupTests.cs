#if DEBUG
[Trait("Category", "Integration")]
public class CleanupTests
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
}
#endif