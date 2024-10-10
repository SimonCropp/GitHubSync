using NGitLab.Models;

[DebuggerDisplay("{Mode} {Type} {Sha} {Path}")]
class GitLabTreeItem(Tree tree) : ITreeItem
{
    public string Mode { get; } = tree.Mode;
    public string Path { get; } = tree.Path;
    public string Name { get; } = tree.Name;
    public string Sha { get; } = tree.Id.ToString();

    public TreeType Type { get; } = tree.Type switch
    {
        ObjectType.blob => TreeType.Blob,
        ObjectType.tree => TreeType.Tree,
        _ => throw new NotImplementedException()
    };
}