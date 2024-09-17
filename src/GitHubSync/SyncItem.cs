public class SyncItem(Parts parts, bool toBeAdded, ResolveTarget? target)
{
    public Parts Parts { get; } = parts;
    public bool ToBeAdded { get; } = toBeAdded;
    public ResolveTarget? Target { get; } = target;
}