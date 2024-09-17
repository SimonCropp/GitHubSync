public static class OctokitEx
{
    public static async Task<List<string>> GetRecursive(IGitProviderGateway gateway, string sourceOwner, string sourceRepository, string? path = null, string? branch = null)
    {
        var items = new List<string>();
        await GetRecursive(gateway, sourceOwner, sourceRepository, path, items, branch ?? "master");
        return items;
    }

    static async Task GetRecursive(IGitProviderGateway gateway, string sourceOwner, string sourceRepository, string? path, List<string> items, string branch)
    {
        foreach (var content in (await gateway.TreeFrom(new(sourceOwner, sourceRepository, TreeEntryTargetType.Tree, branch, path), true))!.Item2.Tree)
        {
            var contentPath = content.Path;
            if (content.Type == TreeType.Blob)
            {
                items.Add(contentPath);
            }

            if (content.Type == TreeType.Tree)
            {
                await GetRecursive(gateway, sourceOwner, sourceRepository, contentPath, items, branch);
            }
        }
    }
}