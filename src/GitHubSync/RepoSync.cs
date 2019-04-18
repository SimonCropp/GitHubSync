using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace GitHubSync
{
    public class RepoSync
    {
        internal List<SyncItem> itemsToSync = new List<SyncItem>();
        Action<string> log;
        List<string> labelsToApplyOnPullRequests;

        internal List<RepositoryInfo> sources = new List<RepositoryInfo>();
        internal List<RepositoryInfo> targets = new List<RepositoryInfo>();

        public RepoSync(Action<string> log = null, List<string> labelsToApplyOnPullRequests = null)
        {
            this.log = log;
            this.labelsToApplyOnPullRequests = labelsToApplyOnPullRequests;
        }

        //public void AddBlob(string path, string target = null)
        //{
        //    AddSourceItem(TreeEntryTargetType.Blob, path, target);
        //}

        //public void RemoveBlob(string path, string target = null)
        //{
        //    RemoveSourceItem(TreeEntryTargetType.Blob, path, target);
        //}

        //public void AddSourceItem(TreeEntryTargetType type, string path, string target = null)
        //{
        //    AddOrRemoveSourceItem(true, type, path, target);
        //}

        //public void RemoveSourceItem(TreeEntryTargetType type, string path, string target = null)
        //{
        //    if (type == TreeEntryTargetType.Tree)
        //    {
        //        throw new NotSupportedException($"Removing a '{nameof(TreeEntryTargetType.Tree)}' isn't supported.");
        //    }

        //    AddOrRemoveSourceItem(false, type, path, target);
        //}

        //public void AddOrRemoveSourceItem(bool toBeAdded, TreeEntryTargetType type, string path, string target)
        //{
        //    Guard.AgainstNullAndEmpty(path, nameof(path));
        //    Guard.AgainstEmpty(target, nameof(target));
        //    itemsToSync.Add(
        //        new SyncItem
        //        {
        //            Parts = new Parts($"{targetOwner}/{targetRepository}", type, targetBranch, path),
        //            ToBeAdded = toBeAdded,
        //            Target = target
        //        });
        //}

        public void AddSourceRepository(RepositoryInfo sourceRepository)
        {
            sources.Add(sourceRepository);
        }

        public void AddTargetRepository(RepositoryInfo targetRepository)
        {
            targets.Add(targetRepository);
        }

        public async Task Sync(SyncOutput syncOutput = SyncOutput.CreatePullRequest)
        {
            foreach (var targetRepository in targets)
            {
                using (var syncer = new Syncer(targetRepository.Credentials, null, log))
                {
                    if (!await syncer.CanSynchronize(targetRepository, syncOutput))
                    {
                        continue;
                    }

                    var targetRepositoryDisplayName = $"{targetRepository.Owner}/{targetRepository.Repository}";
                    var diffs = new List<Mapper>();
                    var includedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var descriptionBuilder = new StringBuilder();
                    descriptionBuilder.AppendLine("This is an automated synchronization PR.");
                    descriptionBuilder.AppendLine();
                    descriptionBuilder.AppendLine("The following source template repositories were used:");

                    // Note: iterate backwards, later registered sources should override earlier registrations
                    for (var i = sources.Count - 1; i >= 0; i--)
                    {
                        var sourceRepository = sources[i];
                        var sourceRepositoryDisplayName = $"{sourceRepository.Owner}/{sourceRepository.Repository}";
                        var itemsToSync = new List<SyncItem>();

                        foreach (var item in await OctokitEx.GetRecursive(sourceRepository.Credentials, sourceRepository.Owner, sourceRepository.Repository))
                        {
                            if (includedPaths.Contains(item))
                            {
                                continue;
                            }

                            includedPaths.Add(item);

                            itemsToSync.Add(new SyncItem
                            {
                                Parts = new Parts($"{sourceRepository.Owner}/{sourceRepository.Repository}",
                                    TreeEntryTargetType.Blob, sourceRepository.Branch, item),
                                ToBeAdded = true,
                                Target = null
                            });
                        }

                        var targetRepositoryToSync = new RepoToSync
                        {
                            Owner = targetRepository.Owner,
                            Repo = targetRepository.Repository,
                            TargetBranch = targetRepository.Branch
                        };

                        var sourceMapper = targetRepositoryToSync.GetMapper(itemsToSync);
                        var diff = await syncer.Diff(sourceMapper);
                        if (diff.ToBeAddedOrUpdatedEntries.Count() > 0 ||
                            diff.ToBeRemovedEntries.Count() > 0)
                        {
                            diffs.Add(diff);

                            descriptionBuilder.AppendLine($"* {sourceRepositoryDisplayName}");
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

                    if (finalDiff.ToBeAddedOrUpdatedEntries.Count() == 0)
                    {
                        log($"Repo {targetRepositoryDisplayName} is in sync");
                        continue;
                    }

                    var sync = await syncer.Sync(finalDiff, syncOutput, labelsToApplyOnPullRequests, descriptionBuilder.ToString());
                    var createdSyncBranch = sync.FirstOrDefault();

                    if (string.IsNullOrEmpty(createdSyncBranch))
                    {
                        log($"Repo {targetRepositoryDisplayName} is in sync");
                    }
                    else
                    {
                        log($"Pull created for {targetRepositoryDisplayName}, click here to review and pull: {createdSyncBranch}");
                    }
                }
            }
        }
    }
}