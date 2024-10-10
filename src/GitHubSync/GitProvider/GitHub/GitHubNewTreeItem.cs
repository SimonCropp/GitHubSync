using Octokit;

class GitHubNewTreeItem(string parentPath, NewTreeItem newTreeItem) : INewTreeItem
{
    public string Mode => newTreeItem.Mode;
    public string Path { get; } = $"{parentPath}{newTreeItem.Path}";
    public string Name => newTreeItem.Path;
    public string Sha => newTreeItem.Sha;
    public TreeType Type { get; } = newTreeItem.Type switch
    {
        Octokit.TreeType.Blob => TreeType.Blob,
        Octokit.TreeType.Tree => TreeType.Tree,
        Octokit.TreeType.Commit => TreeType.Commit,
        _ => throw new NotSupportedException()
    };
}