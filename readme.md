<!--
This file was generate by MarkdownSnippets.
Source File: /readme.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
# <img src="https://raw.github.com/SimonCropp/CaptureSnippet/master/src/icon.png" height="40px"> GitHubSync

A tool to help synchronizing specific files and folders across repositories


## NuGet [![NuGet Status](http://img.shields.io/nuget/v/GitHubSync.svg?longCache=true&style=flat)](https://www.nuget.org/packages/GitHubSync/)

https://nuget.org/packages/GitHubSync/

    PM> Install-Package GitHubSync


## Usage

<!-- snippet: usage -->
```cs
// Create a new RepoSync
var repoSync = new RepoSync(
    log: Console.WriteLine,
    syncMode: SyncMode.IncludeAllByDefault);

// Add source repo(s)
repoSync.AddSourceRepository(new RepositoryInfo(
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
repoSync.AddTargetRepository(new RepositoryInfo(
    credentials: octokitCredentials,
    owner: "UserOrOrg",
    repository: "TargetRepo1",
    branch: "master"));

repoSync.AddTargetRepository(new RepositoryInfo(
    credentials: octokitCredentials,
    owner: "UserOrOrg",
    repository: "TargetRepo2",
    branch: "master"));

// Run the sync
await repoSync.Sync(syncOutput: SyncOutput.MergePullRequest);
```
<sup>[snippet source](/src/Tests/Snippets.cs#L10-L48)</sup>
<!-- endsnippet -->


## Icon

<a href="http://thenounproject.com/term/sync/290/" target="_blank">Sync</a> designed by <a href="http://www.thenounproject.com/edward" target="_blank">Edward Boatman</a> from The Noun Project
