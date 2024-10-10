using System.Net;
using Octokit;

public class GitHubCredentials : ICredentials
{
    public static ICredentials Anonymous { get; } = new GitHubCredentials();

    readonly Credentials credentials;

    private GitHubCredentials() =>
        credentials = Credentials.Anonymous;

    public GitHubCredentials(string token) =>
        credentials = new(token);

    public IGitProviderGateway CreateGateway(IWebProxy? webProxy, Action<string> log) =>
        new GitHubGateway(credentials, webProxy, log);

    public LibGit2Sharp.Credentials CreateLibGit2SharpCredentials() =>
        new LibGit2Sharp.UsernamePasswordCredentials
        {
            // Since we are using tokens, the username should be the token
            Username = credentials.Password,
            Password = string.Empty
        };
}