class TargetTree(Parts root)
{
    public readonly Dictionary<string, TargetTree> SubTreesToUpdate = new();
    public readonly Dictionary<string, Tuple<Parts, Parts>> LeavesToCreate = new();
    public readonly Dictionary<string, Parts> LeavesToDrop = new();
    public readonly Parts Current = root;
    public static string EmptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public void Add(Parts destination, Parts source) =>
        AddOrRemove(destination, source, 0);

    public void Remove(Parts destination) =>
        AddOrRemove(destination, Parts.Empty, 0);

    void AddOrRemove(Parts destination, IParts source, int level)
    {
        var toBeAdded = source is Parts;

        Debug.Assert(
            source is Parts.NullParts || toBeAdded,
            $"Unsupported 'from' type ({source.GetType().FullName}).");

        var segmentedParts = destination.SegmentPartsByNestingLevel(level);

        var segmentedPartsName = segmentedParts.Name!;
        if (destination.NumberOfPathSegments == level + 1)
        {
            if (toBeAdded)
            {
                var leaf = new Tuple<Parts, Parts>(destination, (Parts)source);
                LeavesToCreate.Add(segmentedPartsName, leaf);
            }
            else
            {
                LeavesToDrop.Add(segmentedPartsName, destination);
            }

            return;
        }

        if (!SubTreesToUpdate.TryGetValue(segmentedPartsName, out var targetTree))
        {
            targetTree = new(segmentedParts);
            SubTreesToUpdate.Add(segmentedPartsName, targetTree);
        }

        targetTree.AddOrRemove(destination, source, ++level);
    }
}