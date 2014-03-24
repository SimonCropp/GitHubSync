namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;
    using Octokit;

    public class NewTreeItemComparer : IComparer<NewTreeItem>
    {
        public int Compare(NewTreeItem x, NewTreeItem y)
        {
            return string.Compare(NormalizedPath(x), NormalizedPath(y), StringComparison.Ordinal);
        }

        private string NormalizedPath(NewTreeItem nti)
        {
            if (nti.Mode != "040000")
            {
                return nti.Path;
            }

            return nti.Path + "/";
        }
    }
}
