#nullable enable
namespace GitHubSync;

public class SyncContext
{
    public SyncContext(RepositoryInfo targetRepository, string description, Mapper diff)
    {
        TargetRepository = targetRepository;
        Description = description;
        Diff = diff;
    }

    public RepositoryInfo TargetRepository { get; }

    public string Description { get; }

    public Mapper Diff { get; }
}