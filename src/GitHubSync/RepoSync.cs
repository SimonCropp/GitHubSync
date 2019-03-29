using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitHubSync
{
    public class RepoSync
    {
        List<SyncItem> itemsToSync = new List<SyncItem>();
        Credentials credentials;
        string sourceOwner;
        string sourceRepository;
        string sourceBranch;
        Action<string> log;
        List<string> labelsToApplyOnPullRequests;
        List<RepoToSync> targets = new List<RepoToSync>();

        public RepoSync(Credentials credentials, string sourceOwner, string sourceRepository, string branch, Action<string> log = null, List<string> labelsToApplyOnPullRequests = null)
        {
            Guard.AgainstNull(credentials, nameof(credentials));
            Guard.AgainstNullAndEmpty(sourceOwner, nameof(sourceOwner));
            Guard.AgainstNullAndEmpty(sourceRepository, nameof(sourceRepository));
            Guard.AgainstNullAndEmpty(branch, nameof(branch));
            this.credentials = credentials;
            this.sourceOwner = sourceOwner;
            this.sourceRepository = sourceRepository;
            sourceBranch = branch;
            this.log = log;
            this.labelsToApplyOnPullRequests = labelsToApplyOnPullRequests;
        }

        public void AddBlob(string path, string target = null)
        {
            AddSourceItem(TreeEntryTargetType.Blob, path, target);
        }

        public void RemoveBlob(string path, string target = null)
        {
            RemoveSourceItem(TreeEntryTargetType.Blob, path, target);
        }

        public void AddSourceItem(TreeEntryTargetType type, string path, string target = null)
        {
            AddOrRemoveSourceItem(true, type, path, target);
        }

        public void RemoveSourceItem(TreeEntryTargetType type, string path, string target = null)
        {
            if (type == TreeEntryTargetType.Tree)
            {
                throw new NotSupportedException($"Removing a '{nameof(TreeEntryTargetType.Tree)}' isn't supported.");
            }

            AddOrRemoveSourceItem(false, type, path, target);
        }

        public void AddOrRemoveSourceItem(bool toBeAdded, TreeEntryTargetType type, string path, string target)
        {
            Guard.AgainstNullAndEmpty(path, nameof(path));
            Guard.AgainstEmpty(target, nameof(target));
            itemsToSync.Add(
                new SyncItem
                {
                    Parts = new Parts($"{sourceOwner}/{sourceRepository}", type, sourceBranch, path),
                    ToBeAdded = toBeAdded,
                    Target = target
                });
        }

        public void AddTarget(string repository, string branch = null, Dictionary<string, string> replacementTokens = null)
        {
            AddTarget(sourceOwner, repository, branch, replacementTokens);
        }

        public void AddTarget(string owner, string repository, string branch = null, Dictionary<string, string> replacementTokens = null)
        {
            Guard.AgainstNullAndEmpty(owner, nameof(owner));
            Guard.AgainstNullAndEmpty(repository, nameof(repository));
            Guard.AgainstEmpty(branch, nameof(branch));
            targets.Add(
                new RepoToSync
                {
                    Owner = owner,
                    Repo = repository,
                    TargetBranch = branch,
                    ReplacementTokens = replacementTokens
                });
        }

        public async Task Sync(SyncOutput syncOutput = SyncOutput.CreatePullRequest)
        {
            foreach (var target in targets)
            {
                using (var syncer = new Syncer(credentials, null, log))
                {
                    var mapper = target.GetMapper(itemsToSync);
                    var diff = await syncer.Diff(mapper);
                    var sync = await syncer.Sync(diff, syncOutput, labelsToApplyOnPullRequests);
                    var createdSyncBranch = sync.FirstOrDefault();

                    if (string.IsNullOrEmpty(createdSyncBranch))
                    {
                        log($"Repo {target} is in sync");
                    }
                    else
                    {
                        log($"Pull created for {target}, click here to review and pull: {createdSyncBranch}");
                    }
                }
            }
        }
    }
}