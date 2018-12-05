using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace GitHubSync
{
    public class Syncer : IDisposable
    {
        GitHubGateway gateway;
        Action<string> log;

        public Syncer(
            IEnumerable<Tuple<Credentials, string>> credentialsPerRepos,
            IWebProxy proxy = null,
            Action<string> log = null)
        {
            this.log = log ?? nullLogger;

            gateway = new GitHubGateway(credentialsPerRepos, proxy, log);
        }

        public Syncer(
            Credentials defaultCredentials,
            IWebProxy proxy = null,
            Action<string> log = null)
        {
            this.log = log ?? nullLogger;

            gateway = new GitHubGateway(defaultCredentials, proxy, log);
        }

        static Action<string> nullLogger = _ => { };

        public async Task<Diff> Diff(Mapper input)
        {
            var outMapper = new Diff();

            foreach (var kvp in input)
            {
                var source = kvp.Key;

                log($"Diff - Analyze {source.Type} source '{source.Url}'.");

                var richSource = await EnrichWithShas(source, true).ConfigureAwait(false);

                foreach (var destination in kvp.Value)
                {
                    log($"Diff - Analyze {source.Type} target '{destination.Url}'.");

                    var richDestination = await EnrichWithShas(destination, false).ConfigureAwait(false);

                    if (richSource.Sha == richDestination.Sha)
                    {
                        log($"Diff - No sync required. Matching sha ({richSource.Sha.Substring(0, 7)}) between target '{destination.Url}' and source '{source.Url}.");

                        continue;
                    }

                    log(string.Format("Diff - {4} required. Non-matching sha ({0} vs {1}) between target '{2}' and source '{3}.",
                        richSource.Sha.Substring(0, 7), richDestination.Sha?.Substring(0, 7) ?? "NULL", destination.Url, source.Url, richDestination.Sha == null ? "Creation" : "Updation"));

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
                throw new Exception($"Labels can only be applied in '{SyncOutput.CreatePullRequest}' mode.");
            }

            var t = diff.Transpose();

            var results = new List<string>();

            foreach (var updatesPerOwnerRepositoryBranch in t.Values)
            {
            var branchName = $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
                var root = updatesPerOwnerRepositoryBranch.First().Item1.RootTreePart;
                var tt = new TargetTree(root);

                foreach (var change in updatesPerOwnerRepositoryBranch)
                {
                    var source = change.Item2;
                    var destination = change.Item1;

                    tt.Add(destination, source);
                }

                var btt = await BuildTargetTree(tt).ConfigureAwait(false);

                var parentCommit = await gateway.RootCommitFrom(root).ConfigureAwait(false);

                var c = await gateway.CreateCommit(btt, root.Owner, root.Repository, parentCommit.Sha).ConfigureAwait(false);

                switch (expectedOutput)
                {
                    case SyncOutput.CreateCommit:
                        results.Add($"https://github.com/{root.Owner}/{root.Repository}/commit/{c}");
                        break;

                    case SyncOutput.CreateBranch:
                        branchName = await gateway.CreateBranch(root.Owner, root.Repository, branchName, c).ConfigureAwait(false);
                        results.Add($"https://github.com/{root.Owner}/{root.Repository}/compare/{UrlSanitize(root.Branch)}...{UrlSanitize(branchName)}");
                        break;

                    case SyncOutput.CreatePullRequest:
                        branchName = await gateway.CreateBranch(root.Owner, root.Repository, branchName, c).ConfigureAwait(false);
                        var prNumber = await gateway.CreatePullRequest(root.Owner, root.Repository, branchName, root.Branch).ConfigureAwait(false);
                        gateway.ApplyLabels(root.Owner, root.Repository, prNumber, labels);
                        results.Add($"https://github.com/{root.Owner}/{root.Repository}/pull/{prNumber}");
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
            var treeFrom = await gateway.TreeFrom(tt.Current, false).ConfigureAwait(false);

            NewTree newTree;
            if (treeFrom == null)
            {
                newTree = new NewTree();
            }
            else
            {
                var destinationParentTree = treeFrom.Item2;
                newTree = BuildNewTreeFrom(destinationParentTree);
            }

            foreach (var st in tt.SubTreesToUpdate.Values)
            {
                RemoveTreeItemFrom(newTree, st.Current.Name);
                var sha = await BuildTargetTree(st).ConfigureAwait(false);
                var newTreeItem = new NewTreeItem
                {
                    Mode = "040000",
                    Path = st.Current.Name,
                    Sha = sha,
                    Type = TreeType.Tree
                };

                newTree.Tree.Add(newTreeItem);
            }

            foreach (var l in tt.LeavesToCreate.Values)
            {
                var destination = l.Item1;
                var source = l.Item2;

                RemoveTreeItemFrom(newTree, destination.Name);

                await SyncLeaf(source, destination).ConfigureAwait(false);

                switch (source.Type)
                {
                    case TreeEntryTargetType.Blob:
                        var sourceBlobItem = (await gateway.BlobFrom(source, true).ConfigureAwait(false)).Item2;
                        newTree.Tree.Add(
                            new NewTreeItem
                            {
                                Mode = sourceBlobItem.Mode,
                                Path = destination.Name,
                                Sha = source.Sha,
                                Type = TreeType.Blob
                            });
                        break;

                    case TreeEntryTargetType.Tree:
                        newTree.Tree.Add(
                            new NewTreeItem
                            {
                                Mode = "040000",
                                Path = destination.Name,
                                Sha = source.Sha,
                                Type = TreeType.Tree
                            });
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            return await gateway.CreateTree(newTree, tt.Current.Owner, tt.Current.Repository).ConfigureAwait(false);
        }

        Task SyncLeaf(Parts source, Parts destination)
        {
            var shortSha = source.Sha.Substring(0, 7);
            switch (source.Type)
            {
                case TreeEntryTargetType.Blob:
                    log($"Sync - Determine if Blob '{shortSha}' requires to be created in '{destination.Owner}/{destination.Repository}'.");
                    return SyncBlob(source.Owner, source.Repository, source.Sha, destination.Owner, destination.Repository);
                case TreeEntryTargetType.Tree:
                    log($"Sync - Determine if Tree '{shortSha}' requires to be created in '{destination.Owner}/{destination.Repository}'.");
                    return SyncTree(source, destination.Owner, destination.Repository);
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
                                      Type = treeItem.Type.Value
                                  };

                newTree.Tree.Add(newTreeItem);
            }

            return newTree;
        }

        async Task SyncBlob(string sourceOwner, string sourceRepository, string sha, string destinationOwner, string destinationRepository)
        {
            if (gateway.IsKnownBy<Blob>(sha, destinationOwner, destinationRepository))
            {
                return;
            }

            await gateway.FetchBlob(sourceOwner, sourceRepository, sha).ConfigureAwait(false);
            await gateway.CreateBlob(destinationOwner, destinationRepository, sha).ConfigureAwait(false);
        }

        async Task SyncTree(Parts source, string destinationOwner, string destinationRepository)
        {
            if (gateway.IsKnownBy<TreeResponse>(source.Sha, destinationOwner, destinationRepository))
            {
                return;
            }

            var treeFrom = await gateway.TreeFrom(source, true).ConfigureAwait(false);

            var newTree = new NewTree();

            foreach (var i in treeFrom.Item2.Tree)
            {
                var value = i.Type.Value;
                switch (value)
                {
                    case TreeType.Blob:
                        await SyncBlob(source.Owner, source.Repository, i.Sha, destinationOwner, destinationRepository).ConfigureAwait(false);
                        break;

                    case TreeType.Tree:
                        await SyncTree(treeFrom.Item1.Combine(TreeEntryTargetType.Tree, i.Path, i.Sha), destinationOwner, destinationRepository).ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException();
                }

                newTree.Tree.Add(new NewTreeItem
                {
                    Type = value,
                    Path = i.Path,
                    Sha = i.Sha,
                    Mode = i.Mode
                });
            }

            // ReSharper disable once RedundantAssignment
            var sha = await gateway.CreateTree(newTree, destinationOwner, destinationRepository).ConfigureAwait(false);

            Debug.Assert(source.Sha == sha);
        }

        async Task<Parts> EnrichWithShas(Parts part, bool throwsIfNotFound)
        {
            var outPart = part;

            switch (part.Type)
            {
                case TreeEntryTargetType.Tree:
                    var t = await gateway.TreeFrom(part, throwsIfNotFound).ConfigureAwait(false);

                    if (t != null)
                    {
                        outPart = t.Item1;
                    }

                    break;

                case TreeEntryTargetType.Blob:
                    var b = await gateway.BlobFrom(part, throwsIfNotFound).ConfigureAwait(false);

                    if (b != null)
                    {
                        outPart = b.Item1;
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            return outPart;
        }

        public void Dispose()
        {
            gateway.Dispose();
        }
    }
}