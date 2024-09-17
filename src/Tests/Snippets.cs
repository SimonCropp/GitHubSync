public class Snippets
{
    public static async Task SyncPr(ICredentials octokitCredentials)
    {
        #region usage
        // Create a new RepoSync
        var repoSync = new RepoSync(
            log: Console.WriteLine);

        // Add source repo(s)
        repoSync.AddSourceRepository(new(
            // Valid credentials for the source repo and all target repos
            credentials: octokitCredentials,
            owner: "UserOrOrg",
            repository: "TheSingleSourceRepository",
            branch: "master"));

        // Add sources(s), only allowed when SyncMode == ExcludeAllByDefault
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddBlob("code.cs");

        // Remove sources(s), only allowed when SyncMode == IncludeAllByDefault
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddBlob("code.cs");

        // Add target repo(s)
        repoSync.AddTargetRepository(new(
            credentials: octokitCredentials,
            owner: "UserOrOrg",
            repository: "TargetRepo1",
            branch: "master"));

        repoSync.AddTargetRepository(new(
            credentials: octokitCredentials,
            owner: "UserOrOrg",
            repository: "TargetRepo2",
            branch: "master"));

        // Run the sync
        await repoSync.Sync(syncOutput: SyncOutput.MergePullRequest);

        #endregion
    }
}