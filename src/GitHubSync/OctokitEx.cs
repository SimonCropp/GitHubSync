using Octokit;

public static class OctokitEx
{
    public static async Task<List<string>> GetRecursive(Credentials credentials, string sourceOwner, string sourceRepository, string path = null, string branch = null)
    {
        var items = new List<string>();
        await GetRecursive(credentials, sourceOwner, sourceRepository, path, items, branch);
        return items;
    }

    static Task GetRecursive(Credentials credentials, string sourceOwner, string sourceRepository, string path, List<string> items, string branch)
    {
        var client = new GitHubClient(new ProductHeaderValue("GitHubSync"));

        if (credentials != null)
        {
            client.Credentials = credentials;
        }

        return GetRecursive(client, sourceOwner, sourceRepository, path, items,branch);
    }

    static async Task GetRecursive(GitHubClient client, string sourceOwner, string sourceRepository, string path, List<string> items, string branch)
    {
        foreach (var content in await client.Repository.Content.GetAllContentsEx(sourceOwner, sourceRepository, path,branch))
        {
            var contentPath = content.Path;
            if (content.Type.Value == ContentType.File)
            {
                items.Add(contentPath);
            }

            if (content.Type.Value == ContentType.Dir)
            {
                await GetRecursive(client, sourceOwner, sourceRepository, contentPath, items,branch);
            }
        }
    }

    static Task<IReadOnlyList<RepositoryContent>> GetAllContentsEx(this IRepositoryContentsClient content, string owner, string repo, string path = null, string branch = null)
    {
        if (branch == null)
        {
            if (path != null)
            {
                return content.GetAllContents(owner, repo, path);
            }

            return content.GetAllContents(owner, repo);
        }

        if (path != null)
        {
            return content.GetAllContentsByRef(owner, repo, path, reference: branch);
        }

        return content.GetAllContentsByRef(owner, repo, reference: branch);
    }
}