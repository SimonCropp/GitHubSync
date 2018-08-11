using System;
using Octokit;

public static class CredentialsHelper
{
    static CredentialsHelper()
    {
        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");

        if (githubToken == null)
        {
            Credentials = Credentials.Anonymous;
        }
        else
        {
            Credentials = new Credentials(githubToken);
        }
    }

    public static Credentials Credentials;
}