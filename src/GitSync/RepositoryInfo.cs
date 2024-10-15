public class RepositoryInfo(ICredentials credentials, string owner, string repository, string branch)
{
    public ICredentials Credentials { get; } = credentials;
    public string Owner { get; } = owner;
    public string Repository { get; } = repository;
    public string Branch { get; } = branch;
}