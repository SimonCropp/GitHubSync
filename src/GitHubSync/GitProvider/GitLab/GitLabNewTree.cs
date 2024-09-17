class GitLabNewTree(string parentPath) : INewTree
{
    public INewTreeItemCollection Tree { get; } = new GitLabNewTreeItemCollection(parentPath);
}