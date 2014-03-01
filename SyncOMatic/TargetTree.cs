namespace SyncOMatic.Core
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

        public void Add(Parts dest, Parts source)
        {
            Add(dest, source, 0);
        }

        void Add(Parts dest, Parts source, int level)
        {
            var s = dest.SegmentPartsByNestingLevel(level);

            if (dest.NumberOfPathSegments == level + 1)
            {
                var Leaf = new Tuple<Parts, Parts>(dest, source);
                LeavesToCreate.Add(s.Name, Leaf);
                return;
            }

            TargetTree sb;

            if (!SubTreesToUpdate.TryGetValue(s.Name, out sb))
            {
                sb = new TargetTree(s);
                SubTreesToUpdate.Add(s.Name, sb);
            }

            sb.Add(dest, source, ++level);
        }
    }
}
