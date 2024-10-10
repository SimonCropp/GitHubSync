using Octokit;

class GitHubTree(GitReference gitReference) : ITree
{
    public string Sha { get; } = gitReference.Sha;
}