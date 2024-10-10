using Octokit;

class GitHubNewTreeItemCollection(string parentPath, NewTree parent)
    : List<INewTreeItem>
    , INewTreeItemCollection
{
    public void Add(string mode, string name, string sha, TreeType type)
    {
        var newTreeItem = new NewTreeItem
        {
            Mode = mode,
            Path = name,
            Sha = sha,
            Type = type switch
            {
                TreeType.Blob => Octokit.TreeType.Blob,
                TreeType.Tree => Octokit.TreeType.Tree,
                TreeType.Commit => Octokit.TreeType.Commit,
                _ => throw new NotSupportedException()
            }
        };
        parent.Tree.Add(newTreeItem);

        Add(new GitHubNewTreeItem(parentPath, newTreeItem));
    }
}