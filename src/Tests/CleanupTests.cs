#if DEBUG
[Trait("Category", "Integration")]
public class CleanupTests
{
    [Fact]
    public async Task Run()
    {
        var existing = await Client.GitHubClient.Repository.Branch.GetAll(Client.RepositoryOwner, "GitHubSync.TestRepository");
        foreach (var branch in existing.Where(_ => _.Name.StartsWith("GitHubSync-")))
        {
            await Client.DeleteBranch(branch.Name);
        }
    }
}
#endif