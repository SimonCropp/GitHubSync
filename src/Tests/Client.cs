using System.Linq;
using System.Threading.Tasks;
using Octokit;

public static class Client
{
    public static GitHubClient GitHubClient = new GitHubClient(new ProductHeaderValue("GitHubSync"))
    {
        Credentials = CredentialsHelper.Credentials
    };

    public static async Task DeleteBranch(string branchName)
    {
        var existing = await GitHubClient.Repository.Branch.GetAll("SimonCropp", "GitHubSync.TestRepository");
        if (existing.Any(x=>x.Name==branchName))
        {
            await GitHubClient.Git.Reference.Delete("SimonCropp", "GitHubSync.TestRepository", $"heads/{branchName}");
        }
    }
}