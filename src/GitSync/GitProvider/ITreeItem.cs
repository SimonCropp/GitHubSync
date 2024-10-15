public interface ITreeItem
{
    string Mode { get; }
    string Path { get; }
    string Name { get; }
    string Sha { get; }
    TreeType Type { get; }
}