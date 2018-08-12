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
        string branch;
        Action<LogEntry> loggerCallback;
        private List<RepoToSync> targets = new List<RepoToSync>();

        public RepoSync(Credentials credentials, string sourceOwner, string sourceRepository, string branch, Action<LogEntry> loggerCallback = null)
        {
            this.credentials = credentials;
            this.sourceOwner = sourceOwner;
            this.sourceRepository = sourceRepository;
            this.branch = branch;
            this.loggerCallback = loggerCallback;
        }

        public void AddSourceItem(TreeEntryTargetType type, string path, string target = null)
        {
            itemsToSync.Add(
                new SyncItem
                {
                    Parts = new Parts(sourceOwner + "/" + sourceRepository, type, branch, path),
                    Target = target
                });
        }

        public void AddTarget(string owner, string repository, string branch = null, Dictionary<string, string> replacementTokens = null)
        {
            targets.Add(new RepoToSync
            {
                Owner = owner,
                Repo = repository,
                TargetBranch = branch,
                ReplacementTokens = replacementTokens
            });
        }

        public async Task Sync()
        {
            foreach (var target in targets)
            {
                using (var som = new Syncer(credentials, null, loggerCallback))
                {
                    var diff = await som.Diff(target.GetMapper(itemsToSync));
                    var sync = await som.Sync(diff, SyncOutput.CreatePullRequest, new[] {"Internal refactoring"});
                    var createdSyncBranch = sync.FirstOrDefault();

                    if (string.IsNullOrEmpty(createdSyncBranch))
                    {
                        Console.Out.WriteLine("Repo {0} is in sync", target);
                    }
                    else
                    {
                        Console.Out.WriteLine("Pull created for {0}, click here to review and pull: {1}", target, createdSyncBranch);
                    }
                }
            }
        }
    }
}