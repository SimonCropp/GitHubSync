namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;

    internal class TargetTree
    {
        public Dictionary<string, TargetTree> SubTreesToUpdate { get; private set; }
        public Dictionary<string, Tuple<Parts, Parts>> LeavesToCreate { get; private set; }
        public Parts Current { get; private set; }

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
