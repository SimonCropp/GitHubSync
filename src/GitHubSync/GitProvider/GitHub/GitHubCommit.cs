using Octokit;

class GitHubCommit(Commit commit) : ICommit
{
    public string Sha => commit.Sha;
    public ITree Tree => new GitHubTree(commit.Tree);
}