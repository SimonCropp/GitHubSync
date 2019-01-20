using System;
using Octokit;

public static class CredentialsHelper
{
    static CredentialsHelper()
    {
        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");

        if (githubToken == null)
        {
            throw new Exception("Could not find EnvironmentVariable Octokit_OAuthToken");
        }

        Credentials = new Credentials(githubToken);
    }

    public static Credentials Credentials;
}