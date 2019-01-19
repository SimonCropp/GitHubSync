GitHubSync
==========

![Icon](https://raw.github.com/SimonCropp/GitHubSync/master/src/icon.png)

A tool to help synchronizing specific files and folders across repositories


## NuGet [![NuGet Status](http://img.shields.io/nuget/v/GitHubSync.svg?longCache=true&style=flat)](https://www.nuget.org/packages/GitHubSync/)

https://nuget.org/packages/GitHubSync/

    PM> Install-Package GitHubSync


## Usage

<!-- snippet: usage -->
```cs
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
```
<sup>[snippet source](/src/Tests/Snippets.cs#L10-L37)</sup>
<!-- endsnippet -->


## Icon

<a href="http://thenounproject.com/term/sync/290/" target="_blank">Sync</a> designed by <a href="http://www.thenounproject.com/edward" target="_blank">Edward Boatman</a> from The Noun Project
