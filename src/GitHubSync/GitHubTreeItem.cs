using Octokit;

class GitHubTreeItem(TreeItem treeItem) : ITreeItem
{
    public string Mode => treeItem.Mode;
    public string Path => treeItem.Path;
    public string Name => treeItem.Path;
    public string Sha => treeItem.Sha;
    public TreeType Type => treeItem.Type.Value switch
    {
        Octokit.TreeType.Blob => TreeType.Blob,
        Octokit.TreeType.Tree => TreeType.Tree,
        Octokit.TreeType.Commit => TreeType.Commit,
        _ => throw new NotSupportedException()
    };
}