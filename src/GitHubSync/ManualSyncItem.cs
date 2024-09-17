public class ManualSyncItem(string path, ResolveTarget? target)
{
    public string Path { get; } = path;
    public ResolveTarget? Target { get; } = target;
}