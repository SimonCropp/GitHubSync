using System.Net;

public interface ICredentials
{
    IGitProviderGateway CreateGateway(IWebProxy? webProxy, Action<string> log);

    LibGit2Sharp.Credentials CreateLibGit2SharpCredentials();
}