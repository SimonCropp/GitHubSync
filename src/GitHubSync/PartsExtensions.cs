using GitHubSync;

public static class PartsExtensions
{

    public static Parts Parent(this Parts parts)
    {
        if (parts.Path == null)
        {
            throw new("Cannot escape out of a Tree.");
        }

        var indexOf = parts.Path.LastIndexOf('/');

        var parentPath = indexOf == -1 ? null : parts.Path.Substring(0, indexOf);

        return new(parts.Owner, parts.Repository, TreeEntryTargetType.Tree, parts.Branch, parentPath, null);
    }

    public static Parts Root(this Parts parts) =>
        new(parts.Owner, parts.Repository, TreeEntryTargetType.Tree, parts.Branch, null, null);

    internal static Parts SegmentPartsByNestingLevel(this Parts parts, int level)
    {
        if (parts.Path == null)
        {
            throw new NotSupportedException();
        }

        var s = parts.Path.Split('/').Take(level + 1);

        var p = string.Join("/", s);

        return new(parts.Owner, parts.Repository, TreeEntryTargetType.Tree, parts.Branch, p, null);
    }
}