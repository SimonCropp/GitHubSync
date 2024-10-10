using Octokit;

class GitHubTreeResponse(TreeResponse treeResponse) : ITreeResponse
{
    public string Path => "";
    public IReadOnlyList<ITreeItem> Tree { get; } = treeResponse.Tree.Select(treeItem => new GitHubTreeItem(treeItem)).ToList();
}