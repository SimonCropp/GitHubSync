using GitHubSync;
using Octokit;

public class TempRepoContext :
    IAsyncDisposable
{
    Reference tempBranchReference;
    public string TempBranchName;
    string tempBranchRefName;
    XunitContextBase verifyBase;

    TempRepoContext(Reference tempBranchReference, string tempBranchName, string tempBranchRefName, XunitContextBase verifyBase)
    {
        this.tempBranchReference = tempBranchReference;
        TempBranchName = tempBranchName;
        this.tempBranchRefName = tempBranchRefName;
        this.verifyBase = verifyBase;
    }

    public static async Task<TempRepoContext> Create(string tempBranchName, XunitContextBase verifyBase)
    {
        var newReference = new NewReference($"refs/heads/{tempBranchName}", "af72f8e44eb53d26969b1316491a294f3401f203");

        await Client.DeleteBranch(tempBranchName);
        var tempBranchReference = await Client.GitHubClient.Git.Reference.Create("SimonCropp", "GitHubSync.TestRepository", newReference);
        return new(tempBranchReference, tempBranchName, $"refs/heads/{tempBranchName}", verifyBase);
    }

    public async Task VerifyCommit(UpdateResult updateResult)
    {
        var commit = await Client.GitHubClient.Git.Commit.Get("SimonCropp", "GitHubSync.TestRepository", updateResult.CommitSha);
        await Verify(new
        {
            commit.Message
        });
    }

    public async Task VerifyPullRequest(UpdateResult updateResult)
    {
        var pullRequestId = updateResult.PullRequestId;
        if (pullRequestId == null)
        {
            throw new();
        }

        var files = await Client.GitHubClient.PullRequest.Files("SimonCropp", "GitHubSync.TestRepository", pullRequestId.Value);
        var branch = await Client.GitHubClient.PullRequest.Get("SimonCropp", "GitHubSync.TestRepository", pullRequestId.Value);
        await Verify(new
        {
            branch.Title,
            branch.Body,
            branch.Commits,
            branch.Additions,
            branch.ChangedFiles,
            State = branch.State.StringValue,
            Target = branch.Base.Ref,
            Files = files.Select(x=>new {x.FileName,x.Status})
        });
    }

    public async ValueTask DisposeAsync() =>
        await Client.DeleteBranch(TempBranchName);
}