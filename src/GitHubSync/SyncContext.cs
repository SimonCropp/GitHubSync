namespace GitHubSync
{
    using System.Collections.Generic;

    public class SyncContext
    {
        public SyncContext(RepositoryInfo targetRepository)
        {
            TargetRepository = targetRepository;
        }

        public RepositoryInfo TargetRepository { get; }

        public string Description { get; set; }

        public Mapper Diff { get; set; }
    }
}
