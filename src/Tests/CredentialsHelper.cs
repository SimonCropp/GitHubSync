﻿using Octokit;

public static class CredentialsHelper
{
    static CredentialsHelper()
    {
        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");

        if (githubToken == null)
        {
            throw new("Could not find EnvironmentVariable Octokit_OAuthToken");
        }

        Credentials = new(githubToken);
        GitHubCredentials = new(githubToken);
    }

    public static readonly Credentials Credentials;
    public static readonly GitHubCredentials GitHubCredentials;
}