class RepoToSync
{
    public override string ToString() =>
        $"{Owner}/{Repo}/{TargetBranch}";

    public string Owner { get; }
    public string Repo { get; }
    public string TargetBranch { get; }
    public Dictionary<string, string> ReplacementTokens { get; set; }

    public RepoToSync(string owner, string repo, string targetBranch)
    {
        Owner = owner;
        Repo = repo;
        TargetBranch = targetBranch;
    }

    public Mapper GetMapper(List<SyncItem> syncItems)
    {
        var mapper = new Mapper();

        foreach (var syncItem in syncItems)
        {
            var toPart = new Parts($"{Owner}/{Repo}", syncItem.Parts.Type, TargetBranch, ApplyTargetPathTemplate(syncItem));

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

    string ApplyTargetPathTemplate(SyncItem syncItem)
    {
        var target = syncItem.Target;
        if (string.IsNullOrEmpty(target))
        {
            return syncItem.Parts.Path;
        }

        if (ReplacementTokens != null)
        {
            foreach (var token in ReplacementTokens)
            {
                target = target.Replace(token.Key, token.Value);
            }
        }

        return target;
    }
}