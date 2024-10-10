[DebuggerDisplay("{Mode} {Type} {Sha} {Path}")]
class GitLabNewTreeItem(string mode, string path, string name, string sha, TreeType type) : INewTreeItem
{
    public string Mode { get; } = mode;
    public string Path { get; } = path;
    public string Name { get; } = name;
    public string Sha { get; } = sha;
    public TreeType Type { get; } = type;
}