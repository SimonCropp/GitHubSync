using Octokit;

class GitHubNewTree(string parentPath, NewTree newTree) : INewTree
{
    internal NewTree OriginalTree => newTree;

    public INewTreeItemCollection Tree { get; } = new GitHubNewTreeItemCollection(parentPath, newTree);
}