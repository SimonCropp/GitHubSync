using System;
using System.Threading.Tasks;
using GitHubSync;
using Octokit;

public class Snippets
{
    public async Task SyncPr(Credentials octokitCredentials )
    {
        #region usage
        // Create a new RepoSync
        var repoSync = new RepoSync(
            // Valid credentials for the source repo and all target repos
            credentials: octokitCredentials,
            sourceOwner: "UserOrOrg",
            sourceRepository: "TheSingleSourceRepository",
            branch: "master",
            log: Console.WriteLine);

        // Add sources(s)
        repoSync.AddBlob("sourceFile.txt");
        repoSync.AddBlob("code.cs");

        // Add repo target(s)
        repoSync.AddTarget(
            owner: "UserOrOrg",
            repository: "TargetRepo1",
            branch: "master");
        // Omitting owner will use the sourceOwner passed in to RepoSync
        repoSync.AddTarget(
            repository: "TargetRepo2",
            branch: "master");

        // Run the sync
        await repoSync.Sync(syncOutput: SyncOutput.MergePullRequest);

        #endregion
    }
}