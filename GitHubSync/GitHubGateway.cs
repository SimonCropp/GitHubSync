using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;
using GitHubSync;

class GitHubGateway : IDisposable
{
    Action<LogEntry> logCallBack;
    Dictionary<string, Commit> commitCachePerOwnerRepositoryBranch = new Dictionary<string, Commit>();
    Dictionary<string, Tuple<Parts, TreeItem>> blobCachePerPath = new Dictionary<string, Tuple<Parts, TreeItem>>();
    Dictionary<string, Tuple<Parts, TreeResponse>> treeCachePerPath = new Dictionary<string, Tuple<Parts, TreeResponse>>();
    Dictionary<string, IList<string>> knownBlobsPerRepository = new Dictionary<string, IList<string>>();
    Dictionary<string, IList<string>> knownTreesPerRepository = new Dictionary<string, IList<string>>();
    Dictionary<string, GitHubClient> clientsPerOwnerRepository = new Dictionary<string, GitHubClient>();
    string blobStoragePath;
    const string DEFAULT_CREDENTIALS_KEY = "Default";

    public GitHubGateway(IEnumerable<Tuple<Credentials, string>> credentialsPerRepos, IWebProxy proxy, Action<LogEntry> logCallBack)
    {
        SetupClientCache(credentialsPerRepos, Credentials.Anonymous, proxy);

        this.logCallBack = logCallBack;

        blobStoragePath = Path.Combine(Path.GetTempPath(), $"GitHubSync-{Guid.NewGuid()}");
        Directory.CreateDirectory(blobStoragePath);

        log("Ctor - Create temp blob storage '{0}'.", blobStoragePath);
    }

    public GitHubGateway(Credentials defaultCredentials, IWebProxy proxy, Action<LogEntry> logCallBack)
    {
        SetupClientCache(Enumerable.Empty<Tuple<Credentials, string>>(), defaultCredentials, proxy);

        this.logCallBack = logCallBack;

        blobStoragePath = Path.Combine(Path.GetTempPath(), $"GitHubSync-{Guid.NewGuid()}");
        Directory.CreateDirectory(blobStoragePath);

        log("Ctor - Create temp blob storage '{0}'.", blobStoragePath);
    }

    void SetupClientCache(IEnumerable<Tuple<Credentials, string>> credentialsPerRepos, Credentials defaultCredentials, IWebProxy proxy)
    {
        foreach (var credentialsPerRepo in credentialsPerRepos)
        {
            SetupClientCache(credentialsPerRepo, proxy);
        }

        var client = ClientFrom(defaultCredentials, proxy);

        clientsPerOwnerRepository.Add(DEFAULT_CREDENTIALS_KEY, client);
    }

    void SetupClientCache(Tuple<Credentials, string> credentialsPerRepo, IWebProxy proxy)
    {
        var client = ClientFrom(credentialsPerRepo.Item1, proxy);

        clientsPerOwnerRepository.Add(credentialsPerRepo.Item2, client);
    }

    GitHubClient ClientFrom(Credentials credentials, IWebProxy proxy)
    {
        var connection = new Connection(new ProductHeaderValue("GitHubSync"),
            new HttpClientAdapter(() => HttpMessageHandlerFactory.CreateDefault(proxy)));
        var client = new GitHubClient(connection)
        {
            Credentials = credentials
        };
        return client;
    }

    GitHubClient ClientFor(string owner, string repository)
    {
        var or = string.Join("/", owner, repository);

        if (!clientsPerOwnerRepository.TryGetValue(or, out var client))
        {
            client = clientsPerOwnerRepository[DEFAULT_CREDENTIALS_KEY];
        }

        return client;
    }

    void log(string message, params object[] values)
    {
        logCallBack(new LogEntry(message, values));
    }

    public async Task<Commit> RootCommitFrom(Parts source)
    {
        var orb = $"{source.Owner}/{source.Repository}/{source.Branch}";
        if (commitCachePerOwnerRepositoryBranch.TryGetValue(orb, out var commit))
        {
            return commit;
        }

        log("API Query - Retrieve reference '{0}' details from '{1}/{2}'.",
            "heads/" + source.Branch, source.Owner, source.Repository);

        var client = ClientFor(source.Owner, source.Repository);
        var refBranch = await client.Git.Reference.Get(source.Owner, source.Repository, "heads/" + source.Branch).ConfigureAwait(false);

        log("API Query - Retrieve commit '{0}' details from '{1}/{2}'.",
            refBranch.Object.Sha.Substring(0, 7), source.Owner, source.Repository);

        commit = await client.Git.Commit.Get(source.Owner, source.Repository, refBranch.Object.Sha).ConfigureAwait(false);

        commitCachePerOwnerRepositoryBranch.Add(orb, commit);
        return commit;
    }

    Tuple<Parts, TreeItem> AddToPathCache(Parts parts, TreeItem blobEntry)
    {
        if (blobCachePerPath.TryGetValue(parts.Url, out var blobFrom))
        {
            return blobFrom;
        }

        blobFrom = new Tuple<Parts, TreeItem>(parts, blobEntry);
        blobCachePerPath.Add(parts.Url, blobFrom);
        return blobFrom;
    }

    Tuple<Parts, TreeResponse> AddToPathCache(Parts parts, TreeResponse treeEntry)
    {
        if (treeCachePerPath.TryGetValue(parts.Url, out var treeFrom))
        {
            return treeFrom;
        }

        treeFrom = new Tuple<Parts, TreeResponse>(parts, treeEntry);
        treeCachePerPath.Add(parts.Url, treeFrom);
        return treeFrom;
    }

    public async Task<Tuple<Parts, TreeResponse>> TreeFrom(Parts source, bool throwsIfNotFound)
    {
        Debug.Assert(source.Type == TreeEntryTargetType.Tree);

        if (treeCachePerPath.TryGetValue(source.Url, out var treeResponse))
        {
            return treeResponse;
        }

        string sha;

        if (source.Path == null)
        {
            var commit = await RootCommitFrom(source).ConfigureAwait(false);

            sha = commit.Tree.Sha;
        }
        else
        {
            var parentTreePart = source.ParentTreePart;
            var parentTreeResponse = await TreeFrom(parentTreePart, throwsIfNotFound).ConfigureAwait(false);
            if (parentTreeResponse == null)
            {
                return null;
            }

            var name = source.Path.Split('/').Last();
            var treeItem = parentTreeResponse.Item2.Tree.FirstOrDefault(ti => ti.Type == TreeType.Tree && ti.Path == name);

            if (treeItem == null)
            {
                if (throwsIfNotFound)
                {
                    throw new Exception($"[{source.Type}: {source.Url}] doesn't exist.");
                }

                return null;
            }

            sha = treeItem.Sha;
        }

        log("API Query - Retrieve tree '{0}' ({3}) details from '{1}/{2}'.",
            sha.Substring(0, 7), source.Owner, source.Repository, source.Url);

        var client = ClientFor(source.Owner, source.Repository);
        var tree = await client.Git.Tree.Get(source.Owner, source.Repository, sha).ConfigureAwait(false);
        var parts = new Parts(source.Owner, source.Repository, TreeEntryTargetType.Tree, source.Branch, source.Path, tree.Sha);

        var treeFrom = AddToPathCache(parts, tree);
        AddToKnown<TreeResponse>(parts.Sha, parts.Owner, parts.Repository);

        foreach (var i in tree.Tree)
        {
            switch (i.Type.Value)
            {
                case TreeType.Blob:
                    var p = parts.Combine(TreeEntryTargetType.Blob, i.Path, i.Sha);
                    AddToPathCache(p, i);
                    AddToKnown<Blob>(i.Sha, source.Owner, source.Repository);
                    break;

                case TreeType.Tree:
                    AddToKnown<TreeResponse>(i.Sha, parts.Owner, parts.Repository);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        return treeFrom;
    }

    public async Task<Tuple<Parts, TreeItem>> BlobFrom(Parts source, bool throwsIfNotFound)
    {
        Debug.Assert(source.Type == TreeEntryTargetType.Blob);

        if (blobCachePerPath.TryGetValue(source.Url, out var blobResponse))
        {
            return blobResponse;
        }

        var parent = await TreeFrom(source.ParentTreePart, throwsIfNotFound).ConfigureAwait(false);

        if (parent == null)
        {
            if (throwsIfNotFound)
            {
                throw new Exception($"[{source.ParentTreePart.Type}: {source.ParentTreePart.Url}] doesn't exist.");
            }

            return null;
        }

        var blobName = source.Path.Split('/').Last();
        var blobEntry = parent.Item2.Tree.FirstOrDefault(ti => ti.Type == TreeType.Blob && ti.Path == blobName);

        if (blobEntry == null)
        {
            if (throwsIfNotFound)
            {
                throw new Exception($"[{source.Type}: {source.Url}] doesn't exist.");
            }

            return null;
        }

        var parts = new Parts(source.Owner, source.Repository, TreeEntryTargetType.Blob, source.Branch, source.Path, blobEntry.Sha);

        var blobFrom = AddToPathCache(parts, blobEntry);

        AddToKnown<Blob>(parts.Sha, parts.Owner, parts.Repository);

        return blobFrom;
    }

    void AddToKnown<T>(string sha, string owner, string repository)
    {
        var dic = CacheFor<T>();

        var or = owner + "/" + repository;

        if (!dic.TryGetValue(sha, out var l))
        {
            l = new List<string>();
            dic.Add(sha, l);
        }

        if (l.Contains(or))
        {
            return;
        }

        l.Add(or);
    }

    public bool IsKnownBy<T>(string sha, string owner, string repository)
    {
        var dic = CacheFor<T>();

        if (!dic.TryGetValue(sha, out var l))
        {
            return false;
        }

        var or = owner + "/" + repository;

        return l.Contains(or);
    }

    IDictionary<string, IList<string>> CacheFor<T>()
    {
        IDictionary<string, IList<string>> dic;

        if (typeof(T) == typeof(Blob))
        {
            dic = knownBlobsPerRepository;
        }
        else if (typeof(T) == typeof(TreeResponse))
        {
            dic = knownTreesPerRepository;
        }
        else
        {
            throw new NotSupportedException();
        }

        return dic;
    }

    public async Task<string> CreateCommit(string treeSha, string destinationOwner, string destinationRepository, string parentCommitSha)
    {
        var newCommit = new NewCommit("GitHubSync update", treeSha, new[] {parentCommitSha});

        var client = ClientFor(destinationOwner, destinationRepository);
        var createdCommit = await client.Git.Commit.Create(destinationOwner, destinationRepository, newCommit).ConfigureAwait(false);

        log("API Query - Create commit '{0}' in '{1}/{2}'. -> https://github.com/{1}/{2}/commit/{3}",
            createdCommit.Sha.Substring(0, 7), destinationOwner, destinationRepository, createdCommit.Sha);

        return createdCommit.Sha;
    }

    public async Task<string> CreateTree(NewTree newTree, string destinationOwner, string destinationRepository)
    {
        var client = ClientFor(destinationOwner, destinationRepository);
        var createdTree = await client.Git.Tree.Create(destinationOwner, destinationRepository, newTree).ConfigureAwait(false);

        log("API Query - Create tree '{0}' in '{1}/{2}'.",
            createdTree.Sha.Substring(0, 7), destinationOwner, destinationRepository);

        AddToKnown<TreeResponse>(createdTree.Sha, destinationOwner, destinationRepository);

        return createdTree.Sha;
    }

    public async Task CreateBlob(string owner, string repository, string sha)
    {
        var blobPath = Path.Combine(blobStoragePath, sha);

        var buf = File.ReadAllBytes(blobPath);
        var base64String = Convert.ToBase64String(buf);

        var newBlob = new NewBlob
        {
            Encoding = EncodingType.Base64,
            Content = base64String
        };

        log("API Query - Create blob '{0}' in '{1}/{2}'.", sha.Substring(0, 7), owner, repository);

        var client = ClientFor(owner, repository);
        // ReSharper disable once RedundantAssignment
        var createdBlob = await client.Git.Blob.Create(owner, repository, newBlob).ConfigureAwait(false);
        Debug.Assert(sha == createdBlob.Sha);

        AddToKnown<Blob>(sha, owner, repository);
    }

    public async Task FetchBlob(string owner, string repository, string sha)
    {
        var blobPath = Path.Combine(blobStoragePath, sha);

        if (File.Exists(blobPath))
            return;

        log("API Query - Retrieve blob '{0}' details from '{1}/{2}'.", sha.Substring(0, 7), owner, repository);

        var client = ClientFor(owner, repository);
        var blob = await client.Git.Blob.Get(owner, repository, sha).ConfigureAwait(false);

        switch (blob.Encoding.Value)
        {
            case EncodingType.Utf8:
                File.WriteAllText(blobPath, blob.Content, Encoding.UTF8);
                break;

            case EncodingType.Base64:
                var buf = Convert.FromBase64String(blob.Content);
                File.WriteAllBytes(blobPath, buf);
                break;

            default:
                throw new NotSupportedException();
        }
    }

    public async Task<string> CreateBranch(string owner, string repository, string branchName, string commitSha)
    {
        var newRef = new NewReference("refs/heads/" + branchName, commitSha);

        log("API Query - Create reference '{0}' in '{1}/{2}'.",
            newRef.Ref, owner, repository);

        var client = ClientFor(owner, repository);
        var reference = await client.Git.Reference.Create(owner, repository, newRef).ConfigureAwait(false);
        return reference.Ref.Substring("refs/heads/".Length);
    }

    public async Task<int> CreatePullRequest(string owner, string repository, string branchName, string targetBranchName)
    {
        var client = ClientFor(owner, repository);
        var newPullRequest = new NewPullRequest("GitHubSync update", branchName, targetBranchName);
        var pullRequest = await client.Repository.PullRequest.Create(owner, repository, newPullRequest).ConfigureAwait(false);

        log("API Query - Create pull request '#{0}' in '{1}/{2}'. -> https://github.com/{1}/{2}/pull/{0}",
            pullRequest.Number, owner, repository);

        return pullRequest.Number;
    }

    public void Dispose()
    {
        Directory.Delete(blobStoragePath, true);

        log("Dispose - Remove temp blob storage '{0}'.",
            blobStoragePath);
    }

    public void ApplyLabels(string owner, string repository, int issueNumber, string[] labels)
    {
        Debug.Assert(labels != null);

        log("API Query - Apply labels '{3}' to request '#{0}' in '{1}/{2}'.",
            issueNumber, owner, repository, string.Join(", ", labels));

        var client = ClientFor(owner, repository);
        client.Issue.Labels.AddToIssue(owner, repository, issueNumber, labels)
            .Wait();
    }
}