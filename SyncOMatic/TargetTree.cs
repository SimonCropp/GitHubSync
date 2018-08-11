using System;
using System.Collections.Generic;
using SyncOMatic;

class TargetTree
{
    public readonly Dictionary<string, TargetTree> SubTreesToUpdate;
    public readonly Dictionary<string, Tuple<Parts, Parts>> LeavesToCreate;
    public readonly Parts Current;

    public TargetTree(Parts root)
    {
        Current = root;
        SubTreesToUpdate = new Dictionary<string, TargetTree>();
        LeavesToCreate = new Dictionary<string, Tuple<Parts, Parts>>();
    }

    public void Add(Parts destination, Parts source)
    {
        Add(destination, source, 0);
    }

    void Add(Parts destination, Parts source, int level)
    {
        var s = destination.SegmentPartsByNestingLevel(level);

        if (destination.NumberOfPathSegments == level + 1)
        {
            var leaf = new Tuple<Parts, Parts>(destination, source);
            LeavesToCreate.Add(s.Name, leaf);
            return;
        }

        if (!SubTreesToUpdate.TryGetValue(s.Name, out var targetTree))
        {
            targetTree = new TargetTree(s);
            SubTreesToUpdate.Add(s.Name, targetTree);
        }

        targetTree.Add(destination, source, ++level);
    }
}