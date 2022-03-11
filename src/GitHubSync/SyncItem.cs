#nullable enable
using GitHubSync;

public class SyncItem
{
    public SyncItem(Parts parts, bool toBeAdded, ResolveTarget? target)
    {
        Parts = parts;
        ToBeAdded = toBeAdded;
        Target = target;
    }

    public Parts Parts { get; }
    public bool ToBeAdded { get; }
    public ResolveTarget? Target { get; }
}