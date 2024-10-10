public interface IGitProviderGateway : IDisposable
{
    Task<IUser> GetCurrentUser();
    Task<bool> IsCollaborator(string owner, string name);
    Task<IRepository> Fork(string owner, string name);
    Task DownloadBlob(Parts source, Stream targetStream);
    Task<bool> HasOpenPullRequests(string owner, string name, string prTitle);
    Task<ICommit> RootCommitFrom(Parts source);
    Task<Tuple<Parts, ITreeResponse>?> TreeFrom(Parts source, bool throwsIfNotFound);
    Task<Tuple<Parts, ITreeItem>?> BlobFrom(Parts source, bool throwsIfNotFound);
    bool IsKnownBy<T>(string sha, string owner, string repository);
    Task<string> CreateCommit(string treeSha, string owner, string repo, string parentCommitSha, string branch);
    Task<string> CreateTree(INewTree newTree, string owner, string repo);
    Task CreateBlob(string owner, string repository, string sha);
    Task FetchBlob(string owner, string repository, string sha);
    Task<string> CreateBranch(string owner, string repository, string branchName, string commitSha);
    Task<int> CreatePullRequest(string owner, string repository, string branch, string targetBranch, bool merge, string? description);
    Task<IReadOnlyList<ILabel>> ApplyLabels(string owner, string repository, int issueNumber, string[] labels);
    INewTree CreateNewTree(string? path);
}