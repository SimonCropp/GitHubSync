namespace GitHubSync;

public class ManualSyncItem
{
    public ManualSyncItem(string path, string target)
    {
        Path = path;
        Target = target;
    }
    public string Path { get; }
    public string Target { get; }
}