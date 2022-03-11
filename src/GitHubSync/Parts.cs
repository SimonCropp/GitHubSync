using GitHubSync;

public class Parts : IParts, IEquatable<Parts>
{
    public Parts(string ownerRepository, TreeEntryTargetType type, string branch, string path)
        : this(ownerRepository.Split('/')[0], ownerRepository.Split('/')[1], type, branch, path, null)
    {
    }

    internal Parts(string owner, string repository, TreeEntryTargetType type, string branch, string path, string sha)
    {
        Owner = owner;
        Repository = repository;
        Type = type;
        Branch = branch;
        Path = path;
        Sha = sha;

        Url = string.Join("/", "https://github.com", owner, repository, type.ToString().ToLowerInvariant(), branch);

        if (path == null)
        {
            Name = null;
            NumberOfPathSegments = 0;
        }
        else
        {
            Url = string.Join("/", Url, path);
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
    public string Path { get; }

    public string Name { get; }

    // This doesn't participate as an equality contributor on purpose
    public int NumberOfPathSegments { get; }

    // This doesn't participate as an equality contributor on purpose
    public string Url { get; }

    // This doesn't participate as an equality contributor on purpose
    public string Sha { get; }

    internal Parts Combine(TreeEntryTargetType type, string name, string sha) =>
        new(Owner, Repository, type, Branch, Path == null ? name : Path + "/" + name, sha);

    public bool Equals(Parts other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Owner, other.Owner) && string.Equals(Repository, other.Repository) && Type == other.Type && string.Equals(Path, other.Path);
    }

    public override bool Equals(object obj) =>
        Equals(obj as Parts);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Owner.GetHashCode();
            hashCode = (hashCode * 397) ^ Repository.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) Type;
            hashCode = (hashCode * 397) ^ Branch.GetHashCode();

            if (Path != null)
            {
                hashCode = (hashCode * 397) ^ Path.GetHashCode();
            }

            return hashCode;
        }
    }

    public static bool operator ==(Parts left, Parts right) =>
        Equals(left, right);

    public static bool operator !=(Parts left, Parts right) =>
        !Equals(left, right);

    public class NullParts : IParts
    {
        internal NullParts()
        { }
    }
}