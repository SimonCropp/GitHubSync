public class SyncItem
{
    public SyncItem(Parts parts, bool toBeAdded, string target)
    {
        Parts = parts;
        ToBeAdded = toBeAdded;
        Target = target;
    }
    public Parts Parts { get; }
    public bool ToBeAdded { get; }
    public string Target { get; }
}