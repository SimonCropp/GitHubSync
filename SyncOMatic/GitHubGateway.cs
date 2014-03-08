namespace SyncOMatic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using Octokit;
    using Octokit.Internal;

    class GitHubGateway : IDisposable
    {
        readonly Action<LogEntry> logCallBack;
        readonly IDictionary<string, Commit> commitCachePerOwnerRepositoryBranch = new Dictionary<string, Commit>();
        readonly IDictionary<string, Tuple<Parts, TreeItem>> blobCachePerPath = new Dictionary<string, Tuple<Parts, TreeItem>>();
        readonly IDictionary<string, Tuple<Parts, TreeResponse>> treeCachePerPath = new Dictionary<string, Tuple<Parts, TreeResponse>>();
        readonly IDictionary<string, IList<string>> knownBlobsPerRepository = new Dictionary<string, IList<string>>();
        readonly IDictionary<string, IList<string>> knownTreesPerRepository = new Dictionary<string, IList<string>>();
        readonly IDictionary<string, GitHubClient> clientsPerOwnerRepository = new Dictionary<string, GitHubClient>();
        readonly string blobStoragePath;
        const string DEFAULT_CREDENTIALS_KEY = "Default";

        public GitHubGateway(IEnumerable<Tuple<Credentials, string>> credentialsPerRepos, IWebProxy proxy, Action<LogEntry> logCallBack)
        {
            SetupClientCache(credentialsPerRepos, Credentials.Anonymous, proxy);

            this.logCallBack = logCallBack;

            blobStoragePath = Path.Combine(Path.GetTempPath(), "SyncOMatic-" + Guid.NewGuid());
            Directory.CreateDirectory(blobStoragePath);

            log("Ctor - Create temp blob storage '{0}'.",
                blobStoragePath);
        }

        public GitHubGateway(Credentials defaultCredentials, IWebProxy proxy, Action<LogEntry> logCallBack)
        {
            SetupClientCache(Enumerable.Empty<Tuple<Credentials, string>>(), defaultCredentials, proxy);

            this.logCallBack = logCallBack;

            blobStoragePath = Path.Combine(Path.GetTempPath(), "SyncOMatic-" + Guid.NewGuid());
            Directory.CreateDirectory(blobStoragePath);

            log("Ctor - Create temp blob storage '{0}'.",
                blobStoragePath);
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
            var credentialStore = new InMemoryCredentialStore(credentials);

            var httpClient = new HttpClientAdapter(proxy);

            var connection = new Connection(
                new ProductHeaderValue("SyncOMatic"),
                GitHubClient.GitHubApiUrl,
                credentialStore,
                httpClient,
                new SimpleJsonSerializer());

            var client = new GitHubClient(connection);

            return client;
        }

        GitHubClient ClientFor(string owner, string repository)
        {
            var or = string.Join("/", owner, repository);

            GitHubClient client;
            if (!clientsPerOwnerRepository.TryGetValue(or, out client))
            {
                client = clientsPerOwnerRepository[DEFAULT_CREDENTIALS_KEY];
            }

            return client;
        }

        void log(string message, params object[] values)
        {
            logCallBack(new LogEntry(message, values));
        }

        public Commit RootCommitFrom(Parts source)
        {
            Commit commit;
            var orb = source.Owner + "/" + source.Repository + "/" + source.Branch;
            if (commitCachePerOwnerRepositoryBranch.TryGetValue(orb, out commit))
                return commit;

            log("API Query - Retrieve reference '{0}' details from '{1}/{2}'.",
                "heads/" + source.Branch, source.Owner, source.Repository);

            var client = ClientFor(source.Owner, source.Repository);
            var refBranch = client.GitDatabase.Reference.Get(source.Owner, source.Repository, "heads/" + source.Branch).Result;

            log("API Query - Retrieve commit '{0}' details from '{1}/{2}'.",
                refBranch.Object.Sha.Substring(0, 7), source.Owner, source.Repository);

            commit = client.GitDatabase.Commit.Get(source.Owner, source.Repository, refBranch.Object.Sha).Result;

            commitCachePerOwnerRepositoryBranch.Add(orb, commit);
            return commit;
        }

        Tuple<Parts, TreeItem> AddToPathCache(Parts parts, TreeItem blobEntry)
        {
            Tuple<Parts, TreeItem> blobFrom;

            if (blobCachePerPath.TryGetValue(parts.Url, out blobFrom))
                return blobFrom;

            blobFrom = new Tuple<Parts, TreeItem>(parts, blobEntry);
            blobCachePerPath.Add(parts.Url, blobFrom);
            return blobFrom;
        }

        Tuple<Parts, TreeResponse> AddToPathCache(Parts parts, TreeResponse treeEntry)
        {
            Tuple<Parts, TreeResponse> treeFrom;

            if (treeCachePerPath.TryGetValue(parts.Url, out treeFrom))
                return treeFrom;

            treeFrom = new Tuple<Parts, TreeResponse>(parts, treeEntry);
            treeCachePerPath.Add(parts.Url, treeFrom);
            return treeFrom;
        }

        public Tuple<Parts, TreeResponse> TreeFrom(Parts source, bool throwsIfNotFound)
        {
            Debug.Assert(source.Type == TreeEntryTargetType.Tree);

            Tuple<Parts, TreeResponse> treeResponse;
            if (treeCachePerPath.TryGetValue(source.Url, out treeResponse))
                return treeResponse;

            string sha;

            if (source.Path == null)
            {
                var commit = RootCommitFrom(source);

                sha = commit.Tree.Sha;
            }
            else
            {
                var parentTreePart = source.ParentTreePart;
                var parentTreeResponse = TreeFrom(parentTreePart, throwsIfNotFound);
                if (parentTreeResponse == null)
                    return null;

                var name = source.Path.Split('/').Last();
                var treeItem = parentTreeResponse.Item2.Tree.FirstOrDefault(ti => ti.Type == TreeType.Tree && ti.Path == name);

                if (treeItem == null)
                {
                    if (throwsIfNotFound)
                        throw new MissingSourceException(string.Format("[{0}: {1}] doesn't exist.", source.Type, source.Url));

                    return null;
                }

                sha = treeItem.Sha;
            }

            log("API Query - Retrieve tree '{0}' ({3}) details from '{1}/{2}'.",
                sha.Substring(0, 7), source.Owner, source.Repository, source.Url);

            var client = ClientFor(source.Owner, source.Repository);
            var tree = client.GitDatabase.Tree.Get(source.Owner, source.Repository, sha).Result;
            var parts = new Parts(source.Owner, source.Repository, TreeEntryTargetType.Tree, source.Branch, source.Path, tree.Sha);

            var treeFrom = AddToPathCache(parts, tree);
            AddToKnown<TreeResponse>(parts.Sha, parts.Owner, parts.Repository);

            foreach (var i in tree.Tree)
            {
                switch (i.Type)
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

        public Tuple<Parts, TreeItem> BlobFrom(Parts source, bool throwsIfNotFound)
        {
            Debug.Assert(source.Type == TreeEntryTargetType.Blob);

            Tuple<Parts, TreeItem> blobResponse;
            if (blobCachePerPath.TryGetValue(source.Url, out blobResponse))
                return blobResponse;

            var parent = TreeFrom(source.ParentTreePart, throwsIfNotFound);

            if (parent == null)
            {
                if (throwsIfNotFound)
                    throw new MissingSourceException(string.Format("[{0}: {1}] doesn't exist.", source.ParentTreePart.Type, source.ParentTreePart.Url));

                return null;
            }

            var blobName = source.Path.Split('/').Last();
            var blobEntry = parent.Item2.Tree.FirstOrDefault(ti => ti.Type == TreeType.Blob && ti.Path == blobName);

            if (blobEntry == null)
            {
                if (throwsIfNotFound)
                    throw new MissingSourceException(string.Format("[{0}: {1}] doesn't exist.", source.Type, source.Url));

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

            IList<string> l;
            if (!dic.TryGetValue(sha, out l))
            {
                l = new List<string>();
                dic.Add(sha, l);
            }

            if (l.Contains(or))
                return;

            l.Add(or);
        }

        public bool IsKnownBy<T>(string sha, string owner, string repository)
        {
            var dic = CacheFor<T>();

            IList<string> l;
            if (!dic.TryGetValue(sha, out l))
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

        public string CreateCommit(string treeSha, string destOwner, string destRepository, string parentCommitSha)
        {
            var newCommit = new NewCommit("SyncOMatic update", treeSha, new[] { parentCommitSha });

            var client = ClientFor(destOwner, destRepository);
            var createdCommit = client.GitDatabase.Commit.Create(destOwner, destRepository, newCommit).Result;

            log("API Query - Create commit '{0}' in '{1}/{2}'. -> https://github.com/{1}/{2}/commit/{3}",
                createdCommit.Sha.Substring(0, 7), destOwner, destRepository, createdCommit.Sha);

            return createdCommit.Sha;
        }

        public string CreateTree(NewTree newTree, string destOwner, string destRepository)
        {
            var client = ClientFor(destOwner, destRepository);
            var createdTree = client.GitDatabase.Tree.Create(destOwner, destRepository, newTree).Result;

            log("API Query - Create tree '{0}' in '{1}/{2}'.",
                createdTree.Sha.Substring(0, 7), destOwner, destRepository);

            AddToKnown<TreeResponse>(createdTree.Sha, destOwner, destRepository);

            return createdTree.Sha;
        }

        public void CreateBlob(string owner, string repository, string sha)
        {
            var blobPath = Path.Combine(blobStoragePath, sha);

            var buf = File.ReadAllBytes(blobPath);
            var base64String = Convert.ToBase64String(buf);

            var newBlob = new NewBlob
            {
                Encoding = EncodingType.Base64,
                Content = base64String
            };

            log("API Query - Create blob '{0}' in '{1}/{2}'.",
                sha.Substring(0, 7), owner, repository);

            var client = ClientFor(owner, repository);
            var createdBlob = client.GitDatabase.Blob.Create(owner, repository, newBlob).Result;
            Debug.Assert(sha == createdBlob.Sha);

            AddToKnown<Blob>(sha, owner, repository);
        }

        public void FetchBlob(string owner, string repository, string sha)
        {
            var blobPath = Path.Combine(blobStoragePath, sha);

            if (File.Exists(blobPath))
                return;

            log("API Query - Retrieve blob '{0}' details from '{1}/{2}'.",
                sha.Substring(0, 7), owner, repository);

            var client = ClientFor(owner, repository);
            var blob = client.GitDatabase.Blob.Get(owner, repository, sha).Result;

            switch (blob.Encoding)
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

        public string CreateBranch(string owner, string repository, string branchName, string commitSha)
        {
            var newRef = new NewReference("refs/heads/" + branchName, commitSha);

            log("API Query - Create reference '{0}' in '{1}/{2}'.",
                newRef.Ref, owner, repository);

            var client = ClientFor(owner, repository);
            var reference = client.GitDatabase.Reference.Create(owner, repository, newRef).Result;
            return reference.Ref.Substring("refs/heads/".Length);
        }

        public int CreatePullRequest(string owner, string repository, string branchName, string targetBranchName)
        {
            var client = ClientFor(owner, repository);
            var newPullRequest = new NewPullRequest("SyncOMatic update", branchName, targetBranchName);
            var pullRequest = client.Repository.PullRequest.Create(owner, repository, newPullRequest).Result;

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
            var r = client.Issue.Labels.AddToIssue(owner, repository, issueNumber, labels).Result;
        }
    }
}
