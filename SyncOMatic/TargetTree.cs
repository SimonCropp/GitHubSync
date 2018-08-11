namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;

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
                var Leaf = new Tuple<Parts, Parts>(destination, source);
                LeavesToCreate.Add(s.Name, Leaf);
                return;
            }

            TargetTree sb;

            if (!SubTreesToUpdate.TryGetValue(s.Name, out sb))
            {
                sb = new TargetTree(s);
                SubTreesToUpdate.Add(s.Name, sb);
            }

            sb.Add(destination, source, ++level);
        }
    }
}
