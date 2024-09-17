public class Parts : IParts
{
    public Parts(string owner, string repository, TreeEntryTargetType type, string branch, string? path, string? sha = null, string? mode = null)
    {
        Owner = owner;
        Repository = repository;
        Type = type;
        Branch = branch;
        Path = path;
        Sha = sha;
        Mode = mode;

        Url = string.Join('/', "https://github.com", owner, repository, type.ToString().ToLowerInvariant(), branch);

        if (path == null)
        {
            Name = null;
            NumberOfPathSegments = 0;
        }
        else
        {
            Url = string.Join('/', Url, path);
            var segments = path.Split('/');
            Name = segments.Last();
            NumberOfPathSegments = segments.Length;
        }
    }

    public static readonly NullParts Empty = new();

    public string Owner { get; }
    public string Repository { get; }
    public TreeEntryTargetType Type { get; }
    public string Branch { get; }
    public string? Path { get; }

    public string? Name { get; }

    // This doesn't participate as an equality contributor on purpose
    public int NumberOfPathSegments { get; }

    // This doesn't participate as an equality contributor on purpose
    public string Url { get; }

    // This doesn't participate as an equality contributor on purpose
    public string? Sha { get; }

    // This doesn't participate as an equality contributor on purpose
    public string? Mode { get; }

    public Parts Combine(TreeEntryTargetType type, string name, string sha, string mode) =>
        new(Owner, Repository, type, Branch, Path == null ? name : Path + "/" + name, sha, mode);

    public class NullParts : IParts
    {
        internal NullParts()
        { }
    }
}