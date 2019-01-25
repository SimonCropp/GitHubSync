using System;
using System.Linq;
using GitHubSync;

class Parts : IParts, IEquatable<Parts>
{
    Lazy<Parts> parent;
    Lazy<Parts> root;

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

        parent = new Lazy<Parts>(BuildParent);
        root = new Lazy<Parts>(BuildRoot);
    }

    Parts BuildParent()
    {
        if (Path == null)
        {
            throw new Exception("Cannot escape out of a Tree.");
        }

        var indexOf = Path.LastIndexOf('/');

        var parentPath = indexOf == -1 ? null : Path.Substring(0, indexOf);

        return new Parts(Owner, Repository, TreeEntryTargetType.Tree, Branch, parentPath, null);
    }

    Parts BuildRoot()
    {
        if (Path == null)
        {
            throw new Exception("Cannot escape out of a Tree.");
        }

        return new Parts(Owner, Repository, TreeEntryTargetType.Tree, Branch, null, null);
    }

    public static readonly NullParts Empty = new NullParts();

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

    // This doesn't participate as an equality contributor on purpose
    public Parts ParentTreePart => parent.Value;

    // This doesn't participate as an equality contributor on purpose
    public Parts RootTreePart => root.Value;

    internal Parts Combine(TreeEntryTargetType type, string name, string sha)
    {
        return new Parts(Owner, Repository, type, Branch, Path == null ? name : Path + "/" + name, sha);
    }

    internal Parts SegmentPartsByNestingLevel(int level)
    {
        if (Path == null)
        {
            throw new NotSupportedException();
        }

        var s = Path.Split('/').Take(level + 1);

        var p = string.Join("/", s);

        return new Parts(Owner, Repository, TreeEntryTargetType.Tree, Branch, p, null);
    }

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

    public override bool Equals(object obj)
    {
        return Equals(obj as Parts);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Owner.GetHashCode();
            hashCode = (hashCode * 397) ^ Repository.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) Type;
            hashCode = (hashCode * 397) ^ Branch.GetHashCode();

            if (Path != null)
                hashCode = (hashCode * 397) ^ Path.GetHashCode();

            return hashCode;
        }
    }

    public static bool operator ==(Parts left, Parts right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Parts left, Parts right)
    {
        return !Equals(left, right);
    }

    public class NullParts : IParts
    {
        internal NullParts()
        { }
    }
}