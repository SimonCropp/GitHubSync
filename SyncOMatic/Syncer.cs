namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
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

        public Diff Diff(Mapper input)
        {
            var outMapper = new Diff();

            foreach (var kvp in input)
            {
                var source = kvp.Key;

                log("Diff - Analyze {0} source '{1}'.",
                    source.Type, source.Url);

                var richSource = EnrichWithShas(source, true);

                foreach (var destination in kvp.Value)
                {
                    log("Diff - Analyze {0} target '{1}'.",
                        source.Type, destination.Url);

                    var richDestination = EnrichWithShas(destination, false);

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

        public IEnumerable<string> Sync(Diff diff, SyncOutput expectedOutput, IEnumerable<string> labelsToApplyOnPullRequests = null)
        {
            var labels = labelsToApplyOnPullRequests == null ? new string[]{ } : labelsToApplyOnPullRequests.ToArray();

            if (labels.Any() && expectedOutput != SyncOutput.CreatePullRequest)
            {
                throw new InvalidOperationException(string.Format("Labels can only be applied in '{0}' mode.", SyncOutput.CreatePullRequest));
            }

            var t = diff.Transpose();
            var branchName = "SyncOMatic-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

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

                var btt = BuildTargetTree(tt);

                var parentCommit = gw.RootCommitFrom(root);

                var c = gw.CreateCommit(btt, root.Owner, root.Repository, parentCommit.Sha);

                switch (expectedOutput)
                {
                    case SyncOutput.CreateCommit:
                        yield return "https://github.com/" + root.Owner + "/" + root.Repository + "/commit/" + c;
                        break;

                    case SyncOutput.CreateBranch:
                        branchName = gw.CreateBranch(root.Owner, root.Repository, branchName, c);
                        yield return "https://github.com/" + root.Owner + "/" + root.Repository + "/compare/" + UrlSanitize(root.Branch) + "..." + UrlSanitize(branchName);
                        break;

                    case SyncOutput.CreatePullRequest:
                        branchName = gw.CreateBranch(root.Owner, root.Repository, branchName, c);
                        var prNumber = gw.CreatePullRequest(root.Owner, root.Repository, branchName, root.Branch);
                        gw.ApplyLabels(root.Owner, root.Repository, prNumber, labels);
                        yield return "https://github.com/" + root.Owner + "/" + root.Repository + "/pull/" + prNumber;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        string UrlSanitize(string branch)
        {
            return branch.Replace("/", ";");
        }

        string BuildTargetTree(TargetTree tt)
        {
            var treeFrom = gw.TreeFrom(tt.Current, false);

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
                var sha = BuildTargetTree(st);
                newTree.Tree.Add(new NewTreeItem { Mode = "040000", Path = st.Current.Name, Sha = sha, Type = TreeType.Tree });
            }

            foreach (var l in tt.LeavesToCreate.Values)
            {
                var destination = l.Item1;
                var source = l.Item2;

                RemoveTreeItemFrom(newTree, destination.Name);

                SyncLeaf(source, destination);

                switch (source.Type)
                {
                    case TreeEntryTargetType.Blob:
                        var sourceBlobItem = gw.BlobFrom(source, true).Item2;
                        newTree.Tree.Add(new NewTreeItem { Mode = sourceBlobItem.Mode, Path = destination.Name, Sha = source.Sha, Type = TreeType.Blob });
                        break;

                    case TreeEntryTargetType.Tree:
                        newTree.Tree.Add(new NewTreeItem { Mode = "040000", Path = destination.Name, Sha = source.Sha, Type = TreeType.Tree });
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return gw.CreateTree(newTree, tt.Current.Owner, tt.Current.Repository);
        }

        void SyncLeaf(Parts source, Parts destination)
        {
            switch (source.Type)
            {
                case TreeEntryTargetType.Blob:
                    log("Sync - Determine if Blob '{0}' requires to be created in '{1}/{2}'.",
                        source.Sha.Substring(0, 7), destination.Owner, destination.Repository);

                    SyncBlob(source.Owner, source.Repository, source.Sha, destination.Owner, destination.Repository);
                    break;

                case TreeEntryTargetType.Tree:
                    log("Sync - Determine if Tree '{0}' requires to be created in '{1}/{2}'.",
                        source.Sha.Substring(0, 7), destination.Owner, destination.Repository);

                    SyncTree(source, destination.Owner, destination.Repository);
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

        void SyncBlob(string sourceOwner, string sourceRepository, string sha, string destinationOwner, string destinationRepository)
        {
            if (gw.IsKnownBy<Blob>(sha, destinationOwner, destinationRepository))
                return;

            gw.FetchBlob(sourceOwner, sourceRepository, sha);
            gw.CreateBlob(destinationOwner, destinationRepository, sha);
        }

        void SyncTree(Parts source, string destinationOwner, string destinationRepository)
        {
            if (gw.IsKnownBy<TreeResponse>(source.Sha, destinationOwner, destinationRepository))
                return;

            var treeFrom = gw.TreeFrom(source, true);

            var newTree = new NewTree();

            foreach (var i in treeFrom.Item2.Tree)
            {
                switch (i.Type)
                {
                    case TreeType.Blob:
                        SyncBlob(source.Owner, source.Repository, i.Sha, destinationOwner, destinationRepository);
                        break;

                    case TreeType.Tree:
                        SyncTree(treeFrom.Item1.Combine(TreeEntryTargetType.Tree, i.Path, i.Sha), destinationOwner, destinationRepository);
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

            var sha = gw.CreateTree(newTree, destinationOwner, destinationRepository);

            Debug.Assert(source.Sha == sha);
        }

        Parts EnrichWithShas(Parts part, bool throwsIfNotFound)
        {
            var outPart = part;

            switch (part.Type)
            {
                case TreeEntryTargetType.Tree:
                    var t = gw.TreeFrom(part, throwsIfNotFound);

                    if (t != null)
                        outPart = t.Item1;

                    break;
                case TreeEntryTargetType.Blob:
                    var b = gw.BlobFrom(part, throwsIfNotFound);

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
