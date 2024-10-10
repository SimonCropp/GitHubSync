using Octokit;

class GitHubRepository(Repository repository) : IRepository
{
    public IOwner Owner { get; } = new GitHubOwner(repository.Owner);
    public string CloneUrl => repository.CloneUrl;
}