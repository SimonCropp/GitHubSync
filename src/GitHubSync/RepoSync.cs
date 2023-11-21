#nullable enable
using Octokit;

namespace GitHubSync;

public class RepoSync(
    Action<string>? log = null,
    List<string>? labelsToApplyOnPullRequests = null,
    SyncMode? syncMode = SyncMode.IncludeAllByDefault,
    Credentials? defaultCredentials = null,
    bool skipCollaboratorCheck = false)
{
    Action<string> log = log ?? Console.WriteLine;
    List<ManualSyncItem> manualSyncItems = new();
    List<RepositoryInfo> sources = new();
    List<RepositoryInfo> targets = new();

    public void AddBlob(string path, string? target = null) =>
        AddSourceItem(TreeEntryTargetType.Blob, path, target);

    public void RemoveBlob(string path, string? target = null) =>
        RemoveSourceItem(TreeEntryTargetType.Blob, path, target);

    public void AddSourceItem(TreeEntryTargetType type, string path, string? target = null) =>
        AddOrRemoveSourceItem(true, type, path, target);

    public void RemoveSourceItem(TreeEntryTargetType type, string path, string? target = null) =>
        AddOrRemoveSourceItem(false, type, path, target);

    public void AddOrRemoveSourceItem(bool toBeAdded, TreeEntryTargetType type, string path, string? target)
    {
        if (target == null)
        {
            AddOrRemoveSourceItem(toBeAdded, type, path, (ResolveTarget?) null);
            return;
        }

        AddOrRemoveSourceItem(toBeAdded, type, path, (_, _, _, _) => target);
    }

    public void AddOrRemoveSourceItem(bool toBeAdded, TreeEntryTargetType type, string path, ResolveTarget? target)
    {
        Guard.AgainstNullAndEmpty(path);
        //todo
        //Guard.AgainstEmpty(target, nameof(target));

        if (!toBeAdded && type == TreeEntryTargetType.Tree)
        {
            throw new NotSupportedException($"Removing a '{nameof(TreeEntryTargetType.Tree)}' isn't supported.");
        }

        if (toBeAdded && syncMode == SyncMode.IncludeAllByDefault)
        {
            throw new NotSupportedException($"Adding items is not supported when mode is '{syncMode}'");
        }

        if (!toBeAdded && syncMode == SyncMode.ExcludeAllByDefault)
        {
            throw new NotSupportedException($"Adding items is not supported when mode is '{syncMode}'");
        }

        manualSyncItems.Add(new(path, target));
    }

    public void AddSourceRepository(RepositoryInfo sourceRepository) =>
        sources.Add(sourceRepository);

    public void AddSourceRepository(string owner, string repository, string branch, Credentials? credentials = null) =>
        sources.Add(new(OrDefaultCredentials(credentials), owner, repository, branch));

    public void AddTargetRepository(RepositoryInfo targetRepository) =>
        targets.Add(targetRepository);

    public void AddTargetRepository(string owner, string repository, string branch, Credentials? credentials = null) =>
        targets.Add(new(OrDefaultCredentials(credentials), owner, repository, branch));

    Credentials OrDefaultCredentials(Credentials? credentials) =>
        credentials ?? defaultCredentials ?? throw new("defaultCredentials required");

    public async Task<SyncContext> CalculateSyncContext(RepositoryInfo targetRepository)
    {
        using var syncer = new Syncer(targetRepository.Credentials, null, log);
        var diffs = new List<Mapper>();
        var includedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var descriptionBuilder = new StringBuilder();
        descriptionBuilder.AppendLine("This is an automated synchronization PR.");
        descriptionBuilder.AppendLine();
        descriptionBuilder.AppendLine("The following source template repositories were used:");

        // Note: iterate backwards, later registered sources should override earlier registrations
        for (var i = sources.Count - 1; i >= 0; i--)
        {
            var source = sources[i];
            var displayName = $"{source.Owner}/{source.Repository}";
            var itemsToSync = new List<SyncItem>();

            foreach (var item in await OctokitEx.GetRecursive(source.Credentials, source.Owner, source.Repository, null, source.Branch))
            {
                if (includedPaths.Contains(item))
                {
                    continue;
                }

                includedPaths.Add(item);

                ProcessItem(item, itemsToSync, source);
            }

            var targetRepositoryToSync = new RepoToSync(targetRepository.Owner, targetRepository.Repository, targetRepository.Branch);

            var sourceMapper = targetRepositoryToSync.GetMapper(itemsToSync);
            var diff = await syncer.Diff(sourceMapper);
            if (diff.ToBeAddedOrUpdatedEntries.Any() ||
                diff.ToBeRemovedEntries.Any())
            {
                diffs.Add(diff);

                descriptionBuilder.AppendLine($"* {displayName}");
            }
        }

        var finalDiff = new Mapper();

        foreach (var diff in diffs)
        {
            foreach (var item in diff.ToBeAddedOrUpdatedEntries)
            {
                foreach (var value in item.Value)
                {
                    log($"Mapping '{item.Key.Url}' => '{value.Url}'");

                    finalDiff.Add(item.Key, value);
                }
            }

            // Note: how to deal with items to be removed
        }

        return new(targetRepository, descriptionBuilder.ToString(), finalDiff);
    }

    void ProcessItem(string item, List<SyncItem> itemsToSync, RepositoryInfo source)
    {
        var parts = new Parts(
            source.Owner,
            source.Repository,
            TreeEntryTargetType.Blob,
            source.Branch,
            item);
        var localManualSyncItems = manualSyncItems.Where(_ => item == _.Path).ToList();
        if (localManualSyncItems.Any())
        {
            itemsToSync.AddRange(localManualSyncItems.Select(_ => new SyncItem(parts, syncMode == SyncMode.ExcludeAllByDefault, _.Target)));

            return;
        }

        itemsToSync.Add(new(parts, syncMode == SyncMode.IncludeAllByDefault, null));
    }

    public async Task<IReadOnlyList<UpdateResult>> Sync(SyncOutput syncOutput = SyncOutput.CreatePullRequest)
    {
        var list = new List<UpdateResult>();
        foreach (var targetRepository in targets)
        {
            try
            {
                var targetRepositoryDisplayName = $"{targetRepository.Owner}/{targetRepository.Repository}";

                using var syncer = new Syncer(targetRepository.Credentials, null, log);
                if (!await syncer.CanSynchronize(targetRepository, syncOutput, targetRepository.Branch))
                {
                    continue;
                }

                var syncContext = await CalculateSyncContext(targetRepository);

                if (!syncContext.Diff.ToBeAddedOrUpdatedEntries.Any())
                {
                    log($"Repo {targetRepositoryDisplayName} is in sync");
                    continue;
                }

                var sync = await syncer.Sync(syncContext.Diff, syncOutput, labelsToApplyOnPullRequests, syncContext.Description, skipCollaboratorCheck);
                var createdSyncBranch = sync.FirstOrDefault();

                if (createdSyncBranch == null)
                {
                    log($"Repo {targetRepositoryDisplayName} is in sync");
                }
                else
                {
                    log($"Pull created for {targetRepositoryDisplayName}, click here to review and pull: {createdSyncBranch}");
                    list.Add(createdSyncBranch);
                }
            }
            catch (Exception exception)
            {
                throw new($"Failed to sync Repository:{targetRepository.Repository} Branch:{targetRepository.Branch}", exception);
            }
        }

        return list;
    }
}