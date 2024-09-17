public class SyncContext(RepositoryInfo targetRepository, string description, Mapper diff)
{
    public RepositoryInfo TargetRepository { get; } = targetRepository;

    public string Description { get; } = description;

    public Mapper Diff { get; } = diff;
}