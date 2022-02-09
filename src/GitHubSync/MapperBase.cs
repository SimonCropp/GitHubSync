using GitHubSync;
using System.Collections.ObjectModel;

public abstract class MapperBase
{
    Dictionary<Parts, ICollection<Parts>> toBeAddedOrUpdatedEntries = new();
    List<Parts> toBeRemovedEntries = new();

    protected void AddOrRemove_Internal(IParts from, Parts to)
    {
        switch (from)
        {
            case Parts toAddOrUpdate:
                if (toAddOrUpdate.Type != to.Type)
                {
                    throw new ArgumentException($"Cannot map [{toAddOrUpdate.Type}: {toAddOrUpdate.Url}] to [{to.Type}: {to.Url}]. ");
                }

                if (toBeRemovedEntries.Contains(to))
                {
                    throw new InvalidOperationException($"Cannot add this as the target path '{to.Path}' in branch'{to.Branch}' of '{to.Owner}/{to.Repository}' as it's already scheduled for removal.");
                }

                if (!toBeAddedOrUpdatedEntries.TryGetValue(toAddOrUpdate, out var parts))
                {
                    parts = new List<Parts>();
                    toBeAddedOrUpdatedEntries.Add(toAddOrUpdate, parts);
                }

                parts.Add(to);

                break;

            case Parts.NullParts _:
                if (to.Type == TreeEntryTargetType.Tree)
                {
                    throw new NotSupportedException($"Removing a '{nameof(TreeEntryTargetType.Tree)}' isn't supported.");
                }

                if (toBeAddedOrUpdatedEntries.Values.SelectMany(x => x).Contains(to))
                {
                    throw new InvalidOperationException(
                        $"Cannot remove this as the target path '{to.Path}' in branch '{to.Branch}' of '{to.Owner}/{to.Repository}' as it's already scheduled for addition.");
                }

                if (toBeRemovedEntries.Contains(to))
                {
                    return;
                }

                toBeRemovedEntries.Add(to);

                break;

            default:
                throw new InvalidOperationException($"Unsupported 'from' type ({from.GetType().FullName}).");
        }
    }

    public IEnumerable<KeyValuePair<Parts, IEnumerable<Parts>>> ToBeAddedOrUpdatedEntries
    {
        get
        {
            return toBeAddedOrUpdatedEntries
                .Select(e => new KeyValuePair<Parts, IEnumerable<Parts>>(e.Key, e.Value));
        }
    }

    public IEnumerable<Parts> ToBeRemovedEntries => new ReadOnlyCollection<Parts>(toBeRemovedEntries);

    public IDictionary<string, IList<Tuple<Parts, IParts>>> Transpose()
    {
        var parts = new Dictionary<string, IList<Tuple<Parts, IParts>>>();

        foreach (var kvp in toBeAddedOrUpdatedEntries)
        {
            var source = kvp.Key;

            foreach (var destination in kvp.Value)
            {
                var orb = $"{destination.Owner}/{destination.Repository}/{destination.Branch}";

                if (!parts.TryGetValue(orb, out var items))
                {
                    items = new List<Tuple<Parts, IParts>>();
                    parts.Add(orb, items);
                }

                items.Add(new Tuple<Parts, IParts>(destination, source));
            }
        }

        foreach (var destination in toBeRemovedEntries)
        {
            var orb = $"{destination.Owner}/{destination.Repository}/{destination.Branch}";

            if (!parts.TryGetValue(orb, out var items))
            {
                items = new List<Tuple<Parts, IParts>>();
                parts.Add(orb, items);
            }

            items.Add(new Tuple<Parts, IParts>(destination, Parts.Empty));
        }

        return parts;
    }
}