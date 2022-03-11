#nullable enable
namespace GitHubSync;

using Octokit;

public class RepositoryInfo
{
    public RepositoryInfo(Credentials credentials, string owner, string repository, string branch)
    {
        Credentials = credentials;
        Owner = owner;
        Repository = repository;
        Branch = branch;
    }

    public Credentials Credentials { get; }
    public string Owner { get; }
    public string Repository { get; }
    public string Branch { get; }
}