using System.Runtime.CompilerServices;
using Octokit;

public class TempRepoContext :
    IAsyncDisposable
{
    public string TempBranchName;

    TempRepoContext(string tempBranchName) =>
        TempBranchName = tempBranchName;

    public static async Task<TempRepoContext> Create([CallerMemberName] string tempBranchName = "")
    {
        var newReference = new NewReference($"refs/heads/{tempBranchName}", "af72f8e44eb53d26969b1316491a294f3401f203");

        await Client.DeleteBranch(tempBranchName);
        await Client.GitHubClient.Git.Reference.Create(Client.RepositoryOwner, "GitHubSync.TestRepository", newReference);
        return new(tempBranchName);
    }

    public async Task VerifyCommit(UpdateResult updateResult)
    {
        var commit = await Client.GitHubClient.Git.Commit.Get(Client.RepositoryOwner, "GitHubSync.TestRepository", updateResult.CommitSha);
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

        var files = await Client.GitHubClient.PullRequest.Files(Client.RepositoryOwner, "GitHubSync.TestRepository", pullRequestId.Value);
        var branch = await Client.GitHubClient.PullRequest.Get(Client.RepositoryOwner, "GitHubSync.TestRepository", pullRequestId.Value);
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