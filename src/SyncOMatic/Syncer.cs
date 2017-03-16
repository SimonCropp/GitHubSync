﻿namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Octokit;

    public class Syncer : IDisposable
    {
        GitHubGateway gw;
        Action<LogEntry> logCallBack;

        // TODO: Maybe expose api rate info per connection?
        // TODO: Add SyncResult (BranchCreated, PullRequestCreated, PullRequestMerged) + url

        public Syncer(
            IEnumerable<Tuple<Credentials, string>> credentialsPerRepos,
            IWebProxy proxy = null,
            Action<LogEntry> loggerCallback = null)
        {
            logCallBack = loggerCallback ?? NullLogger;

            gw = new GitHubGateway(credentialsPerRepos, proxy, logCallBack);
        }

        public Syncer(
            Credentials defaultCredentials,
            IWebProxy proxy = null,
            Action<LogEntry> loggerCallback = null)
        {
            logCallBack = loggerCallback ?? NullLogger;

            gw = new GitHubGateway(defaultCredentials, proxy, logCallBack);
        }

        static Action<LogEntry> NullLogger = _ => { };

        private void log(string message, params object[] values)
        {
            logCallBack(new LogEntry(message, values));
        }

        public async Task<Diff> Diff(Mapper input)
        {
            var outMapper = new Diff();

            foreach (var kvp in input)
            {
                var source = kvp.Key;

                log("Diff - Analyze {0} source '{1}'.",
                    source.Type, source.Url);

                var richSource = await EnrichWithShas(source, true).IgnoreWaitContext();

                foreach (var destination in kvp.Value)
                {
                    log("Diff - Analyze {0} target '{1}'.",
                        source.Type, destination.Url);

                    var richDestination = await EnrichWithShas(destination, false).IgnoreWaitContext();

                    if (richSource.Sha == richDestination.Sha)
                    {
                        log("Diff - No sync required. Matching sha ({0}) between target '{1}' and source '{2}.",
                            richSource.Sha.Substring(0, 7), destination.Url, source.Url);

                        continue;
                    }

                    log("Diff - {4} required. Non-matching sha ({0} vs {1}) between target '{2}' and source '{3}.",
                        richSource.Sha.Substring(0, 7), richDestination.Sha == null ? "NULL" : richDestination.Sha.Substring(0, 7), destination.Url, source.Url, richDestination.Sha == null ? "Creation" : "Updation");

                    outMapper.Add(richSource, richDestination);
                }
            }

            return outMapper;
        }

        public async Task<IEnumerable<string>> Sync(Diff diff, SyncOutput expectedOutput, IEnumerable<string> labelsToApplyOnPullRequests = null)
        {
            var labels = labelsToApplyOnPullRequests?.ToArray() ?? new string[] { };

            if (labels.Any() && expectedOutput != SyncOutput.CreatePullRequest)
            {
                throw new InvalidOperationException($"Labels can only be applied in '{SyncOutput.CreatePullRequest}' mode.");
            }

            var t = diff.Transpose();
            var branchName = "SyncOMatic-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

            var results = new List<string>();

            foreach (var updatesPerOwnerRepositoryBranch in t.Values)
            {
                var root = updatesPerOwnerRepositoryBranch.First().Item1.RootTreePart;
                var tt = new TargetTree(root);

                foreach (var change in updatesPerOwnerRepositoryBranch)
                {
                    var source = change.Item2;
                    var destination = change.Item1;

                    tt.Add(destination, source);
                }

                var btt = await BuildTargetTree(tt).IgnoreWaitContext();

                var parentCommit = await gw.RootCommitFrom(root).IgnoreWaitContext();

                var c = await gw.CreateCommit(btt, root.Owner, root.Repository, parentCommit.Sha).IgnoreWaitContext();

                switch (expectedOutput)
                {
                    case SyncOutput.CreateCommit:
                        results.Add("https://github.com/" + root.Owner + "/" + root.Repository + "/commit/" + c);
                        break;

                    case SyncOutput.CreateBranch:
                        branchName = await gw.CreateBranch(root.Owner, root.Repository, branchName, c).IgnoreWaitContext();
                        results.Add("https://github.com/" + root.Owner + "/" + root.Repository + "/compare/" + UrlSanitize(root.Branch) + "..." + UrlSanitize(branchName));
                        break;

                    case SyncOutput.CreatePullRequest:
                        branchName = await gw.CreateBranch(root.Owner, root.Repository, branchName, c).IgnoreWaitContext();
                        var prNumber = await gw.CreatePullRequest(root.Owner, root.Repository, branchName, root.Branch).IgnoreWaitContext();
                        gw.ApplyLabels(root.Owner, root.Repository, prNumber, labels);
                        results.Add("https://github.com/" + root.Owner + "/" + root.Repository + "/pull/" + prNumber);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return results;
        }

        string UrlSanitize(string branch)
        {
            return branch.Replace("/", ";");
        }

        async Task<string> BuildTargetTree(TargetTree tt)
        {
            var treeFrom = await gw.TreeFrom(tt.Current, false).IgnoreWaitContext();

            NewTree newTree;
            if (treeFrom != null)
            {
                var destinationParentTree = treeFrom.Item2;
                newTree = BuildNewTreeFrom(destinationParentTree);
            }
            else
            {
                newTree = new NewTree();
            }

            foreach (var st in tt.SubTreesToUpdate.Values)
            {
                RemoveTreeItemFrom(newTree, st.Current.Name);
                var sha = await BuildTargetTree(st).IgnoreWaitContext();
                newTree.Tree.Add(new NewTreeItem { Mode = "040000", Path = st.Current.Name, Sha = sha, Type = TreeType.Tree });
            }

            foreach (var l in tt.LeavesToCreate.Values)
            {
                var destination = l.Item1;
                var source = l.Item2;

                RemoveTreeItemFrom(newTree, destination.Name);

                await SyncLeaf(source, destination).IgnoreWaitContext();

                switch (source.Type)
                {
                    case TreeEntryTargetType.Blob:
                        var sourceBlobItem = (await gw.BlobFrom(source, true).IgnoreWaitContext()).Item2;
                        newTree.Tree.Add(new NewTreeItem { Mode = sourceBlobItem.Mode, Path = destination.Name, Sha = source.Sha, Type = TreeType.Blob });
                        break;

                    case TreeEntryTargetType.Tree:
                        newTree.Tree.Add(new NewTreeItem { Mode = "040000", Path = destination.Name, Sha = source.Sha, Type = TreeType.Tree });
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return await gw.CreateTree(newTree, tt.Current.Owner, tt.Current.Repository).IgnoreWaitContext();
        }

        async Task SyncLeaf(Parts source, Parts destination)
        {
            switch (source.Type)
            {
                case TreeEntryTargetType.Blob:
                    log("Sync - Determine if Blob '{0}' requires to be created in '{1}/{2}'.",
                        source.Sha.Substring(0, 7), destination.Owner, destination.Repository);

                    await SyncBlob(source.Owner, source.Repository, source.Sha, destination.Owner, destination.Repository).IgnoreWaitContext();
                    break;

                case TreeEntryTargetType.Tree:
                    log("Sync - Determine if Tree '{0}' requires to be created in '{1}/{2}'.",
                        source.Sha.Substring(0, 7), destination.Owner, destination.Repository);

                    await SyncTree(source, destination.Owner, destination.Repository).IgnoreWaitContext();
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        void RemoveTreeItemFrom(NewTree tree, string name)
        {
            var existing = tree.Tree.SingleOrDefault(ti => ti.Path == name);

            if (existing == null)
            {
                return;
            }

            tree.Tree.Remove(existing);
        }

        static NewTree BuildNewTreeFrom(TreeResponse destinationParentTree)
        {
            var newTree = new NewTree();

            foreach (var treeItem in destinationParentTree.Tree)
            {
                var newTreeItem = new NewTreeItem
                                  {
                                      Mode = treeItem.Mode,
                                      Path = treeItem.Path,
                                      Sha = treeItem.Sha,
                                      Type = treeItem.Type
                                  };

                newTree.Tree.Add(newTreeItem);
            }

            return newTree;
        }

        async Task SyncBlob(string sourceOwner, string sourceRepository, string sha, string destinationOwner, string destinationRepository)
        {
            if (gw.IsKnownBy<Blob>(sha, destinationOwner, destinationRepository))
                return;

            await gw.FetchBlob(sourceOwner, sourceRepository, sha).ConfigureAwait(false);
            await gw.CreateBlob(destinationOwner, destinationRepository, sha).ConfigureAwait(false);
        }

        async Task SyncTree(Parts source, string destinationOwner, string destinationRepository)
        {
            if (gw.IsKnownBy<TreeResponse>(source.Sha, destinationOwner, destinationRepository))
                return;

            var treeFrom = await gw.TreeFrom(source, true).ConfigureAwait(false);

            var newTree = new NewTree();

            foreach (var i in treeFrom.Item2.Tree)
            {
                switch (i.Type)
                {
                    case TreeType.Blob:
                        await SyncBlob(source.Owner, source.Repository, i.Sha, destinationOwner, destinationRepository).IgnoreWaitContext();
                        break;

                    case TreeType.Tree:
                        await SyncTree(treeFrom.Item1.Combine(TreeEntryTargetType.Tree, i.Path, i.Sha), destinationOwner, destinationRepository).IgnoreWaitContext();
                        break;

                    default:
                        throw new NotSupportedException();
                }

                newTree.Tree.Add(new NewTreeItem
                {
                    Type = i.Type,
                    Path = i.Path,
                    Sha = i.Sha,
                    Mode = i.Mode
                });
            }

            // ReSharper disable once RedundantAssignment
            var sha = await gw.CreateTree(newTree, destinationOwner, destinationRepository).ConfigureAwait(false);

            Debug.Assert(source.Sha == sha);
        }

        async Task<Parts> EnrichWithShas(Parts part, bool throwsIfNotFound)
        {
            var outPart = part;

            switch (part.Type)
            {
                case TreeEntryTargetType.Tree:
                    var t = await gw.TreeFrom(part, throwsIfNotFound).IgnoreWaitContext();

                    if (t != null)
                        outPart = t.Item1;

                    break;

                case TreeEntryTargetType.Blob:
                    var b = await gw.BlobFrom(part, throwsIfNotFound).IgnoreWaitContext();

                    if (b != null)
                        outPart = b.Item1;

                    break;

                default:
                    throw new NotSupportedException();
            }

            return outPart;
        }

        public void Dispose()
        {
            gw.Dispose();
        }
    }
}