public static class GitCredentialFactory
{
    public static ICredentials? Create()
    {
        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");

        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            return new GitHubCredentials(githubToken);
        }

        var gitlabToken = Environment.GetEnvironmentVariable("GitLab_OAuthToken");
        var gitlabHostUrl = Environment.GetEnvironmentVariable("GitLab_HostUrl");

        if (!string.IsNullOrWhiteSpace(gitlabToken))
        {
            return new GitLabCredentials(gitlabHostUrl ?? "https://gitlab.com", gitlabToken);
        }

        return null;
    }
}