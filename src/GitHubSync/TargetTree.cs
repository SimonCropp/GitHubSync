using System;
using System.Collections.Generic;
using System.Diagnostics;

class TargetTree
{
    public Dictionary<string, TargetTree> SubTreesToUpdate;
    public Dictionary<string, Tuple<Parts, Parts>> LeavesToCreate;
    public Dictionary<string, Parts> LeavesToDrop;
    public Parts Current;
    public static string EmptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public TargetTree(Parts root)
    {
        Current = root;
        SubTreesToUpdate = new Dictionary<string, TargetTree>();
        LeavesToCreate = new Dictionary<string, Tuple<Parts, Parts>>();
        LeavesToDrop = new Dictionary<string, Parts>();
    }

    public void Add(Parts destination, Parts source)
    {
        AddOrRemove(destination, source, 0);
    }

    public void Remove(Parts destination)
    {
        AddOrRemove(destination, Parts.Empty, 0);
    }

    void AddOrRemove(Parts destination, IParts source, int level)
    {
        var toBeAdded = source is Parts;

        Debug.Assert(
            source is Parts.NullParts || toBeAdded,
            $"Unsupported 'from' type ({source.GetType().FullName}).");

        var s = destination.SegmentPartsByNestingLevel(level);

        if (destination.NumberOfPathSegments == level + 1)
        {
            if (toBeAdded)
            {
                var leaf = new Tuple<Parts, Parts>(destination, source as Parts);
                LeavesToCreate.Add(s.Name, leaf);
            }
            else
            {
                LeavesToDrop.Add(s.Name, destination);
            }

            return;
        }

        if (!SubTreesToUpdate.TryGetValue(s.Name, out var targetTree))
        {
            targetTree = new TargetTree(s);
            SubTreesToUpdate.Add(s.Name, targetTree);
        }

        targetTree.AddOrRemove(destination, source, ++level);
    }
}