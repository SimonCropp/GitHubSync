using Octokit;

public static class Client
{
    public const string RepositoryOwner = "SimonCropp";

    public static readonly GitHubClient GitHubClient = new(new ProductHeaderValue("GitHubSync"))
    {
        Credentials = CredentialsHelper.Credentials
    };

    public static async Task DeleteBranch(string branchName)
    {
        var existing = await GitHubClient.Repository.Branch.GetAll(RepositoryOwner, "GitHubSync.TestRepository");
        if (existing.Any(_ => _.Name == branchName))
        {
            await GitHubClient.Git.Reference.Delete(RepositoryOwner, "GitHubSync.TestRepository", $"heads/{branchName}");
        }
    }
}