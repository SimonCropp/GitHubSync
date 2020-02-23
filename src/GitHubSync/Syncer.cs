using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GitHubSync;
using Octokit;

class Syncer :
    IDisposable
{
    const string PullRequestTitle = "GitHubSync update";

    GitHubGateway gateway;
    Action<string> log;
    Credentials credentials;

    public Syncer(
        Credentials credentials,
        IWebProxy proxy = null,
        Action<string> log = null)
    {
        this.log = log ?? nullLogger;

        this.credentials = credentials;
        gateway = new GitHubGateway(credentials, proxy, log);
    }

    static Action<string> nullLogger = _ => { };

    internal async Task<Mapper> Diff(Mapper input)
    {
        Guard.AgainstNull(input, nameof(input));
        var outMapper = new Mapper();

        foreach (var kvp in input.ToBeAddedOrUpdatedEntries)
        {
            var source = kvp.Key;

            log($"Diff - Analyze {source.Type} source '{source.Url}'.");

            var richSource = await EnrichWithShas(source, true);

            foreach (var destination in kvp.Value)
            {
                log($"Diff - Analyze {source.Type} target '{destination.Url}'.");

                var richDestination = await EnrichWithShas(destination, false);

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

        foreach (var p in input.ToBeRemovedEntries)
        {
            outMapper.Remove(p);
        }

        return outMapper;
    }

    internal async Task<bool> CanSynchronize(RepositoryInfo targetRepository, SyncOutput expectedOutput)
    {
        if (expectedOutput == SyncOutput.CreatePullRequest ||
            expectedOutput == SyncOutput.MergePullRequest)
        {
            var hasOpenPullRequests = await gateway.HasOpenPullRequests(targetRepository.Owner, targetRepository.Repository, PullRequestTitle);
            if (hasOpenPullRequests)
            {
                log("Cannot create pull request, there is an existing open pull request, close or merge that first");
                return false;
            }
        }

        return true;
    }

    internal async Task<IReadOnlyList<UpdateResult>> Sync(
        Mapper diff,
        SyncOutput expectedOutput,
        IEnumerable<string> labelsToApplyOnPullRequests = null,
        string description = null)
    {
        Guard.AgainstNull(diff, nameof(diff));
        Guard.AgainstNull(expectedOutput, nameof(expectedOutput));
        var labels = labelsToApplyOnPullRequests?.ToArray() ?? new string[] { };

        if (labels.Any() && expectedOutput != SyncOutput.CreatePullRequest)
        {
            throw new Exception($"Labels can only be applied in '{SyncOutput.CreatePullRequest}' mode.");
        }

        var t = diff.Transpose();

        var results = new List<UpdateResult>();

        foreach (var updatesPerOwnerRepositoryBranch in t.Values)
        {
            var updates = await ProcessUpdates(expectedOutput, updatesPerOwnerRepositoryBranch, labels, description);
            results.Add(updates);
        }

        return results;
    }

    async Task<UpdateResult> ProcessUpdates(
        SyncOutput expectedOutput,
        IList<Tuple<Parts, IParts>> updatesPerOwnerRepositoryBranch,
        string[] labels,
        string description)
    {
        var branchName = $"GitHubSync-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var root = updatesPerOwnerRepositoryBranch.First().Item1.RootTreePart;

        string commitSha;

        var isCollaborator = await gateway.IsCollaborator(root.Owner, root.Repository);
        if (isCollaborator)
        {
            commitSha = await ProcessUpdatesInTargetRepository(root, updatesPerOwnerRepositoryBranch);
        }
        else
        {
            log("User is not a collaborator, need to create a fork");

            if (expectedOutput != SyncOutput.CreatePullRequest)
            {
                throw new NotSupportedException($"User is not a collaborator, sync output '{expectedOutput}' is not supported, only creating PRs is supported");
            }

            commitSha = await ProcessUpdatesInFork(root, branchName, updatesPerOwnerRepositoryBranch);
        }

        if (expectedOutput == SyncOutput.CreateCommit)
        {
            return new UpdateResult
            {
                Url = $"https://github.com/{root.Owner}/{root.Repository}/commit/{commitSha}",
                CommitSha = commitSha
            };
        }

        if (expectedOutput == SyncOutput.CreateBranch)
        {
            branchName = await gateway.CreateBranch(root.Owner, root.Repository, branchName, commitSha);
            return new UpdateResult
            {
                Url = $"https://github.com/{root.Owner}/{root.Repository}/compare/{UrlSanitize(root.Branch)}...{UrlSanitize(branchName)}",
                CommitSha = commitSha,
                BranchName = branchName
            };
        }

        if (expectedOutput == SyncOutput.CreatePullRequest || expectedOutput == SyncOutput.MergePullRequest)
        {
            var merge = expectedOutput == SyncOutput.MergePullRequest;
            var prSourceBranch = branchName;

            if (isCollaborator)
            {
                branchName = await gateway.CreateBranch(root.Owner, root.Repository, branchName, commitSha);
            }
            else
            {
                // Never auto-merge
                merge = false;

                var forkedRepository = await gateway.Fork(root.Owner, root.Repository);
                prSourceBranch = $"{forkedRepository.Owner.Login}:{prSourceBranch}";
            }

            var prNumber = await gateway.CreatePullRequest(root.Owner, root.Repository, prSourceBranch, root.Branch, merge, description);

            if (isCollaborator)
            {
                await gateway.ApplyLabels(root.Owner, root.Repository, prNumber, labels);
            }

            return new UpdateResult
            {
                Url = $"https://github.com/{root.Owner}/{root.Repository}/pull/{prNumber}",
                CommitSha = commitSha,
                BranchName = root.Branch,
                PullRequestId = prNumber
            };
        }

        throw new NotSupportedException();
    }

    async Task<string> ProcessUpdatesInTargetRepository(Parts root, IList<Tuple<Parts, IParts>> updatesPerOwnerRepositoryBranch)
    {
        var tt = new TargetTree(root);

        foreach (var change in updatesPerOwnerRepositoryBranch)
        {
            var source = change.Item2;
            var destination = change.Item1;

            switch (source)
            {
                case Parts toAddOrUpdate:
                    tt.Add(destination, toAddOrUpdate);
                    break;

                case Parts.NullParts _:
                    tt.Remove(destination);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported 'from' type ({source.GetType().FullName}).");
            }
        }

        var btt = await BuildTargetTree(tt);

        var parentCommit = await gateway.RootCommitFrom(root);

        var commitSha = await gateway.CreateCommit(btt, root.Owner, root.Repository, parentCommit.Sha);
        return commitSha;
    }

    async Task<string> ProcessUpdatesInFork(Parts root, string temporaryBranchName, IList<Tuple<Parts, IParts>> updatesPerOwnerRepositoryBranch)
    {
        var forkedRepository = await gateway.Fork(root.Owner, root.Repository);

        var temporaryPath = Path.Combine(Path.GetTempPath(), "GitHubSync", root.Owner, root.Repository);

        if (Directory.Exists(temporaryPath))
        {
            Directory.Delete(temporaryPath, true);
        }

        Directory.CreateDirectory(temporaryPath);

        // Step 1: clone the fork
        var repositoryPath = LibGit2Sharp.Repository.Clone(forkedRepository.CloneUrl, temporaryPath, new LibGit2Sharp.CloneOptions
        {
            BranchName = root.Branch
        });

        var currentUser = await gateway.GetCurrentUser();
        var commitSignature = new LibGit2Sharp.Signature(currentUser.Name, currentUser.Email ?? "hidden@protected.com", DateTimeOffset.Now);

        using var repository = new LibGit2Sharp.Repository(repositoryPath);
        // Step 2: ensure upstream
        var remotes = repository.Network.Remotes;
        var originRemote = remotes["origin"];
        var upstreamRemote = remotes["upstream"] ?? remotes.Add("upstream", $"https://github.com/{root.Owner}/{root.Repository}");

        LibGit2Sharp.Commands.Fetch(repository, "upstream", upstreamRemote.FetchRefSpecs.Select(x => x.Specification), null, null);

        // Step 3: create local branch
        var tempBranch = repository.Branches.Add(temporaryBranchName, "HEAD");
        repository.Branches.Update(tempBranch, b =>
        {
            b.Remote = originRemote.Name;
            b.UpstreamBranch = tempBranch.CanonicalName;
            //b.Upstream = $"refs/heads/{temporaryBranchName}";
            //b.UpstreamBranch = $""
        });

        LibGit2Sharp.Commands.Checkout(repository, tempBranch);

        // Step 4: ensure we have the latest
        var upstreamMasterBranch = repository.Branches["upstream/master"];

        repository.Merge(upstreamMasterBranch, commitSignature, new LibGit2Sharp.MergeOptions());

        // Step 5: create delta
        foreach (var change in updatesPerOwnerRepositoryBranch)
        {
            var source = change.Item2;
            var destination = change.Item1;
            var fullDestination = Path.Combine(temporaryPath, destination.Path.Replace('/', Path.DirectorySeparatorChar));

            switch (source)
            {
                case Parts _:
                    // Directly download raw bytes into file
                    await using (var fileStream = new FileStream(fullDestination, System.IO.FileMode.Create))
                    {
                        await gateway.DownloadBlob((Parts)source, fileStream);
                    }
                    break;

                case Parts.NullParts _:
                    File.Delete(fullDestination);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported 'from' type ({source.GetType().FullName}).");
            }
        }

        // Step 6: stage all files
        LibGit2Sharp.Commands.Stage(repository, "*");

        // Step 7: create & push commit
        var commit = repository.Commit("Apply GitHubSync changes", commitSignature, commitSignature,
            new LibGit2Sharp.CommitOptions());

        repository.Network.Push(tempBranch, new LibGit2Sharp.PushOptions
        {
            CredentialsProvider = (_url, _user, _cred) => new LibGit2Sharp.UsernamePasswordCredentials
            {
                // Since we are using tokens, the username should be the token
                Username = credentials.Password,
                Password = string.Empty
            }
        });

        return commit.Sha;
    }

    string UrlSanitize(string branch)
    {
        return branch.Replace("/", ";");
    }

    async Task<string> BuildTargetTree(TargetTree tt)
    {
        var treeFrom = await gateway.TreeFrom(tt.Current, false);

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
            var sha = await BuildTargetTree(st);

            if (sha == TargetTree.EmptyTreeSha)
            {
                // Resulting tree contains no items
                continue;
            }

            var newTreeItem = new NewTreeItem
            {
                Mode = "040000",
                Path = st.Current.Name,
                Sha = sha,
                Type = TreeType.Tree
            };

            newTree.Tree.Add(newTreeItem);
        }

        foreach (var l in tt.LeavesToDrop.Values)
        {
            RemoveTreeItemFrom(newTree, l.Name);
        }

        foreach (var l in tt.LeavesToCreate.Values)
        {
            var destination = l.Item1;
            var source = l.Item2;

            RemoveTreeItemFrom(newTree, destination.Name);

            await SyncLeaf(source, destination);

            switch (source.Type)
            {
                case TreeEntryTargetType.Blob:
                    var sourceBlobItem = (await gateway.BlobFrom(source, true)).Item2;
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

        if (newTree.Tree.Count == 0)
        {
            return TargetTree.EmptyTreeSha;
        }

        return await gateway.CreateTree(newTree, tt.Current.Owner, tt.Current.Repository);
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

        await gateway.FetchBlob(sourceOwner, sourceRepository, sha);
        await gateway.CreateBlob(destinationOwner, destinationRepository, sha);
    }

    async Task SyncTree(Parts source, string destinationOwner, string destinationRepository)
    {
        if (gateway.IsKnownBy<TreeResponse>(source.Sha, destinationOwner, destinationRepository))
        {
            return;
        }

        var treeFrom = await gateway.TreeFrom(source, true);

        var newTree = new NewTree();

        foreach (var i in treeFrom.Item2.Tree)
        {
            var value = i.Type.Value;
            switch (value)
            {
                case TreeType.Blob:
                    await SyncBlob(source.Owner, source.Repository, i.Sha, destinationOwner, destinationRepository);
                    break;

                case TreeType.Tree:
                    await SyncTree(treeFrom.Item1.Combine(TreeEntryTargetType.Tree, i.Path, i.Sha), destinationOwner, destinationRepository);
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
        var sha = await gateway.CreateTree(newTree, destinationOwner, destinationRepository);

        Debug.Assert(source.Sha == sha);
    }

    async Task<Parts> EnrichWithShas(Parts part, bool throwsIfNotFound)
    {
        var outPart = part;

        switch (part.Type)
        {
            case TreeEntryTargetType.Tree:
                var t = await gateway.TreeFrom(part, throwsIfNotFound);
                if (t != null)
                {
                    outPart = t.Item1;
                }

                break;

            case TreeEntryTargetType.Blob:
                var b = await gateway.BlobFrom(part, throwsIfNotFound);
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

namespace GitHubSync
{
}