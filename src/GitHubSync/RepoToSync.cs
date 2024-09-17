class RepoToSync(string owner, string repo, string targetBranch)
{
    public override string ToString() =>
        $"{Owner}/{Repo}/{TargetBranch}";

    public string Owner { get; } = owner;
    public string Repo { get; } = repo;
    public string TargetBranch { get; } = targetBranch;

    public Mapper GetMapper(List<SyncItem> syncItems)
    {
        var mapper = new Mapper();

        foreach (var syncItem in syncItems)
        {
            var toPart = new Parts(Owner, Repo, syncItem.Parts.Type, TargetBranch, ApplyTargetPathTemplate(syncItem));

            if (syncItem.ToBeAdded)
            {
                mapper.Add(syncItem.Parts, toPart);
            }
            else
            {
                mapper.Remove(toPart);
            }
        }

        return mapper;
    }

    static string? ApplyTargetPathTemplate(SyncItem syncItem)
    {
        var target = syncItem.Target;
        var parts = syncItem.Parts;
        if (target == null)
        {
            return parts.Path;
        }

        return target(parts.Owner, parts.Repository, parts.Branch, parts.Path);
    }
}