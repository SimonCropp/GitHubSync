using NGitLab;

static class GitLabHelpers
{
    static Dictionary<string, int> projectIds = new();

    public static async Task<int> GetProjectId(this IGitLabClient client, string owner, string name)
    {
        var key = $"{owner}/{name}";

        if (!projectIds.TryGetValue(key, out var id))
        {
            id = (await client
                .Projects
                .GetByNamespacedPathAsync(key))
                .Id;

            projectIds[key] = id;
        }

        return id;
    }
}