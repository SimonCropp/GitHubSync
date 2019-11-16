namespace GitHubSync
{
    public class UpdateResult
    {
        public string Url { get; set; }
        public string CommitSha { get; set; }
        public string BranchName { get; set; }
        public int PullRequestId { get; set; }
    }
}