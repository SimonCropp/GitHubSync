#nullable enable
namespace GitHubSync;

public class ManualSyncItem
{
    public ManualSyncItem(string path, ResolveTarget? target)
    {
        Path = path;
        Target = target;
    }

    public string Path { get; }
    public ResolveTarget? Target { get; }
}