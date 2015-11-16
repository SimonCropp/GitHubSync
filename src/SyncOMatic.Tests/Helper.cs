using System;
using Octokit;

public static class Helper
{
    // From https://github.com/octokit/octokit.net/blob/master/Octokit.Tests.Integration/Helper.cs

    static readonly Lazy<Credentials> credentialsThunk = new Lazy<Credentials>(() =>
    {
        var githubUsername = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME");

        var githubToken = Environment.GetEnvironmentVariable("OCTOKIT_OAUTHTOKEN");

        if (githubToken != null)
        {
            return new Credentials(githubToken);
        }

        var githubPassword = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD");

        if (githubUsername == null || githubPassword == null)
        {
            return Credentials.Anonymous;
        }

        return new Credentials(githubUsername, githubPassword);
    });

    public static Credentials Credentials => credentialsThunk.Value;
    
}