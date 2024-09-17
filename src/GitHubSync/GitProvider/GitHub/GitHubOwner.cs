using Octokit;

class GitHubOwner(User repositoryOwner) : IOwner
{
    public string Login => repositoryOwner.Location;
}