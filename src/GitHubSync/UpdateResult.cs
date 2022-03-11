namespace GitHubSync;

public class UpdateResult
{
    public UpdateResult(string url, string commitSha, string branchName, int? pullRequestId)
    {
        Url = url;
        CommitSha = commitSha;
        BranchName = branchName;
        PullRequestId = pullRequestId;
    }

    public string Url { get; }
    public string CommitSha { get; }
    public string BranchName { get; }
    public int? PullRequestId { get; }
}