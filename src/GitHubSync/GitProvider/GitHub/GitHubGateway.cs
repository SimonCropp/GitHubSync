using System.Net;
using Octokit;
using Octokit.Internal;

class GitHubGateway
    : IGitProviderGateway
{
    Action<string> log;
    Dictionary<string, GitHubCommit> commitCachePerOwnerRepositoryBranch = new();
    Dictionary<string, Tuple<Parts, ITreeItem>> blobCachePerPath = new();
    Dictionary<string, Tuple<Parts, ITreeResponse>> treeCachePerPath = new();
    Dictionary<string, IList<string>> knownBlobsPerRepository = new();
    Dictionary<string, IList<string>> knownTreesPerRepository = new();
    GitHubClient client;
    string blobStoragePath;

    public GitHubGateway(Credentials credentials, IWebProxy? proxy, Action<string> log)
    {
        client = ClientFrom(credentials, proxy);

        this.log = log;

        blobStoragePath = Path.Combine(Path.GetTempPath(), $"GitHubSync-{Guid.NewGuid()}");
        Directory.CreateDirectory(blobStoragePath);

        log($"Ctor - Create temp blob storage '{blobStoragePath}'.");
    }

    static GitHubClient ClientFrom(Credentials credentials, IWebProxy? proxy)
    {
        var connection = new Connection(
            new("GitHubSync"),
            new HttpClientAdapter(() => HttpMessageHandlerFactory.CreateDefault(proxy)));

        return new(connection)
        {
            Credentials = credentials
        };
    }

    public async Task<IUser> GetCurrentUser()
    {
        var currentUser = await client.User.Current();
        return new GitHubUser(currentUser);
    }

    public async Task<bool> IsCollaborator(string owner, string name)
    {
        // Note: checking whether a user is a collaborator requires push access
        var allRepos = await client.Repository.GetAllForCurrent();
        return allRepos.Any(_ => string.Equals(_.FullName, $"{owner}/{name}", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IRepository> Fork(string owner, string name)
    {
        var apiConnection = new ApiConnection(client.Connection);
        var forkClient = new RepositoryForksClient(apiConnection);

        return new GitHubRepository(await forkClient.Create(owner, name, new()));
    }

    public async Task DownloadBlob(Parts source, Stream targetStream)
    {
        var downloadUrl = $"https://raw.githubusercontent.com/{source.Owner}/{source.Repository}/{source.Branch}/{source.Path}";

        log($"Downloading blob from '{downloadUrl}'");

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        await using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
        await streamToReadFrom.CopyToAsync(targetStream);
    }

    public async Task<bool> HasOpenPullRequests(string owner, string name, string prTitle)
    {
        var pullRequests = await client.PullRequest.GetAllForRepository(owner, name);

        return pullRequests.Any(_ => string.Equals(_.Title, prTitle, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ICommit> RootCommitFrom(Parts source)
    {
        var orb = $"{source.Owner}/{source.Repository}/{source.Branch}";
        if (commitCachePerOwnerRepositoryBranch.TryGetValue(orb, out var commit))
        {
            return commit;
        }

        log($"API Query - Retrieve reference '{"heads/" + source.Branch}' details from '{source.Owner}/{source.Repository}'.");

        var refBranch = await client.Git.Reference.Get(source.Owner, source.Repository, "heads/" + source.Branch);

        log($"API Query - Retrieve commit '{refBranch.Object.Sha.Substring(0, 7)}' details from '{source.Owner}/{source.Repository}'.");

        commit = new(await client.Git.Commit.Get(source.Owner, source.Repository, refBranch.Object.Sha));

        commitCachePerOwnerRepositoryBranch.Add(orb, commit);
        return commit;
    }

    Tuple<Parts, ITreeItem> AddToPathCache(Parts parts, ITreeItem blobEntry)
    {
        if (blobCachePerPath.TryGetValue(parts.Url, out var blobFrom))
        {
            return blobFrom;
        }

        blobFrom = new(parts, blobEntry);
        blobCachePerPath.Add(parts.Url, blobFrom);
        return blobFrom;
    }

    Tuple<Parts, ITreeResponse> AddToPathCache(Parts parts, ITreeResponse treeEntry)
    {
        if (treeCachePerPath.TryGetValue(parts.Url, out var treeFrom))
        {
            return treeFrom;
        }

        treeFrom = new(parts, treeEntry);
        treeCachePerPath.Add(parts.Url, treeFrom);
        return treeFrom;
    }

    public async Task<Tuple<Parts, ITreeResponse>?> TreeFrom(Parts source, bool throwsIfNotFound)
    {
        Debug.Assert(source.Type == TreeEntryTargetType.Tree);

        if (treeCachePerPath.TryGetValue(source.Url, out var treeResponse))
        {
            return treeResponse;
        }

        string sha;

        if (source.Path == null)
        {
            var commit = await RootCommitFrom(source);

            sha = commit.Tree.Sha;
        }
        else
        {
            var parentTreeResponse = await TreeFrom(source.Parent(), throwsIfNotFound);
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
                    throw new($"[{source.Type}: {source.Url}] doesn't exist.");
                }

                return null;
            }

            sha = treeItem.Sha;
        }

        log(string.Format("API Query - Retrieve tree '{0}' ({3}) details from '{1}/{2}'.",
            sha.Substring(0, 7), source.Owner, source.Repository, source.Url));

        var tree = await client.Git.Tree.Get(source.Owner, source.Repository, sha);
        var parts = new Parts(source.Owner, source.Repository, TreeEntryTargetType.Tree, source.Branch, source.Path, tree.Sha);

        var treeFrom = AddToPathCache(parts, new GitHubTreeResponse(tree));
        AddToKnown<ITreeResponse>(parts.Sha!, parts.Owner, parts.Repository);

        foreach (var i in tree.Tree)
        {
            switch (i.Type.Value)
            {
                case Octokit.TreeType.Blob:
                    var p = parts.Combine(TreeEntryTargetType.Blob, i.Path, i.Sha, i.Mode);
                    AddToPathCache(p, new GitHubTreeItem(i));
                    AddToKnown<IBlob>(i.Sha, source.Owner, source.Repository);
                    break;

                case Octokit.TreeType.Tree:
                    AddToKnown<ITreeResponse>(i.Sha, parts.Owner, parts.Repository);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        return treeFrom;
    }

    public async Task<Tuple<Parts, ITreeItem>?> BlobFrom(Parts source, bool throwsIfNotFound)
    {
        Debug.Assert(source.Type == TreeEntryTargetType.Blob);

        if (blobCachePerPath.TryGetValue(source.Url, out var blobResponse))
        {
            return blobResponse;
        }

        var parentPart = source.Parent();
        var parentTree = await TreeFrom(parentPart, throwsIfNotFound);

        if (parentTree == null)
        {
            if (throwsIfNotFound)
            {
                throw new($"[{parentPart.Type}: {parentPart.Url}] doesn't exist.");
            }

            return null;
        }

        var sourcePath = source.Path!;

        var blobName = sourcePath.Split('/').Last();
        var blobEntry = parentTree.Item2.Tree.FirstOrDefault(ti => ti.Type == TreeType.Blob && ti.Path == blobName);

        if (blobEntry == null)
        {
            if (throwsIfNotFound)
            {
                throw new($"[{source.Type}: {source.Url}] doesn't exist.");
            }

            return null;
        }

        var parts = new Parts(source.Owner, source.Repository, TreeEntryTargetType.Blob, source.Branch, sourcePath, blobEntry.Sha, blobEntry.Mode);

        var blobFrom = AddToPathCache(parts, blobEntry);

        AddToKnown<IBlob>(parts.Sha!, parts.Owner, parts.Repository);

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

        if (typeof(T) == typeof(IBlob))
        {
            dic = knownBlobsPerRepository;
        }
        else if (typeof(T) == typeof(ITreeResponse))
        {
            dic = knownTreesPerRepository;
        }
        else
        {
            throw new NotSupportedException();
        }

        return dic;
    }

    public async Task<string> CreateCommit(string treeSha, string owner, string repo, string parentCommitSha, string branch)
    {
        var newCommit = new NewCommit($"GitHubSync update - {branch}", treeSha, [parentCommitSha]);

        var createdCommit = await client.Git.Commit.Create(owner, repo, newCommit);

        log(string.Format("API Query - Create commit '{0}' in '{1}/{2}'. -> https://github.com/{1}/{2}/commit/{3}",
            createdCommit.Sha.Substring(0, 7), owner, repo, createdCommit.Sha));

        return createdCommit.Sha;
    }

    public async Task<string> CreateTree(INewTree newTree, string owner, string repo)
    {
        var createdTree = await client.Git.Tree.Create(owner, repo, ((GitHubNewTree)newTree).OriginalTree);

        log($"API Query - Create tree '{createdTree.Sha.Substring(0, 7)}' in '{owner}/{repo}'.");

        AddToKnown<ITreeResponse>(createdTree.Sha, owner, repo);

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

        log($"API Query - Create blob '{sha.Substring(0, 7)}' in '{owner}/{repository}'.");

        // ReSharper disable once RedundantAssignment
        var createdBlob = await client.Git.Blob.Create(owner, repository, newBlob);
        Debug.Assert(sha == createdBlob.Sha);

        AddToKnown<IBlob>(sha, owner, repository);
    }

    public async Task FetchBlob(string owner, string repository, string sha)
    {
        var blobPath = Path.Combine(blobStoragePath, sha);

        if (File.Exists(blobPath))
        {
            return;
        }

        log($"API Query - Retrieve blob '{sha.Substring(0, 7)}' details from '{owner}/{repository}'.");

        var blob = await client.Git.Blob.Get(owner, repository, sha);

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

        log($"API Query - Create reference '{newRef.Ref}' in '{owner}/{repository}'.");

        var reference = await client.Git.Reference.Create(owner, repository, newRef);
        return reference.Ref.Substring("refs/heads/".Length);
    }

    public async Task<int> CreatePullRequest(
        string owner,
        string repository,
        string branch,
        string targetBranch,
        bool merge,
        string? description)
    {
        var newPullRequest = new NewPullRequest($"GitHubSync update - {targetBranch}", branch, targetBranch);

        if (!string.IsNullOrWhiteSpace(description))
        {
            newPullRequest.Body = description;
        }

        var pullRequest = await client.Repository.PullRequest.Create(owner, repository, newPullRequest);
        var prUrl = $"https://github.com/{owner}/{repository}/pull/{pullRequest.Number}";

        log($"API Query - Create pull request '#{pullRequest.Number}' in '{owner}/{repository}'. -> {prUrl}");

        if (merge)
        {
            if (!pullRequest.Mergeable.GetValueOrDefault(true))
            {
                throw new($"PR not mergable: {prUrl}");
            }

            log($"API Query - Merge pull request '#{pullRequest.Number}' in '{owner}/{repository}'. -> {prUrl}");
            await client.Repository.PullRequest.Merge(owner, repository, pullRequest.Number, new());
        }

        return pullRequest.Number;
    }

    public void Dispose()
    {
        Directory.Delete(blobStoragePath, true);

        log($"Dispose - Remove temp blob storage '{blobStoragePath}'.");
    }

    public async Task<IReadOnlyList<ILabel>> ApplyLabels(string owner, string repository, int issueNumber, string[] labels)
    {
        log(string.Format("API Query - Apply labels '{3}' to request '#{0}' in '{1}/{2}'.", issueNumber, owner, repository, string.Join(", ", labels)));

        var gitLabels = await client.Issue.Labels.AddToIssue(owner, repository, issueNumber, labels);

        return gitLabels.Select(_ => new GitHubLabel(_)).ToList();
    }

    public INewTree CreateNewTree(string? path) =>
        new GitHubNewTree(string.IsNullOrWhiteSpace(path) ? "" : $"{path}/", new());
}