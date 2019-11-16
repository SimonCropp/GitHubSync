using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

public class TempRepoContext :
    IAsyncDisposable
{
    Reference tempBranchReference;
    public string TempBranchName;
    string tempBranchRefName;

    TempRepoContext(Reference tempBranchReference, string tempBranchName, string tempBranchRefName)
    {
        this.tempBranchReference = tempBranchReference;
        TempBranchName = tempBranchName;
        this.tempBranchRefName = tempBranchRefName;
    }

    public static async Task<TempRepoContext> Create(string tempBranchName)
    {
        var newReference = new NewReference($"refs/heads/{tempBranchName}", "af72f8e44eb53d26969b1316491a294f3401f203");

        await Client.DeleteBranch(tempBranchName);
        var tempBranchReference = await Client.GitHubClient.Git.Reference.Create("SimonCropp", "GitHubSync.TestRepository", newReference);
        return new TempRepoContext(tempBranchReference, tempBranchName, $"refs/heads/{tempBranchName}");
    }

    public async Task VerifyPullRequest(string name)
    {
        var branch = await Client.GitHubClient.Repository.Branch.Get("SimonCropp", "GitHubSync.TestRepository", name);
        ObjectApprover.Verify(branch);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DeleteBranch(TempBranchName);
    }
}