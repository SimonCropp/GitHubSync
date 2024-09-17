class GitLabNewTreeItemCollection(string parentPath)
    : List<INewTreeItem>
    , INewTreeItemCollection
{
    public void Add(string mode, string name, string sha, TreeType type) =>
        Add(new GitLabNewTreeItem(mode, $"{parentPath}{name}", name, sha, type));
}