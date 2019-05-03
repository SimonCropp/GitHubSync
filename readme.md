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

## Usage via configuration

This package allows reading the configuration from a file. This allows customization of the templates and repositories without
having to recompile any code.

### Configuration definition

The configuration format is yaml. There should be 1 to n number of templates and 1 to n number of (target) repositories.

```yaml
templates:
  - name: [template name]
    url: [repository url of the template]
    branch: [branch to use, defaults to `master`]
    
repositories:
  - name: [repository name]
    url: [repository url of the target repository]
    branch: [target branch, defaults to `master`]
    autoMerge: [true / false, true is only used when user is allowed to merge PRs on the target repository]
    templates:
      - [list of template names to use in the order to apply]
```

### Example

```yaml
templates:
  - name: geertvanhorrik
    url: https://github.com/geertvanhorrik/repositorytemplate
    branch: master
  - name: catel
    url: https://github.com/Catel/RepositoryTemplate.Components
    branch: master
  - name: wildgums-components-public
    url: https://github.com/wildgums/RepositoryTemplate.Components.Public
    branch: master

repositories:
  - name: CsvHelper
    url: https://github.com/JoshClose/CsvHelper
    branch: master
    autoMerge: false
    templates:
      - geertvanhorrik

  - name: Catel
    url: https://github.com/catel/catel
    branch: develop
    autoMerge: true
    templates:
      - geertvanhorrik
      - catel

  - name: Orc.Controls
    url: https://github.com/wildgums/orc.controls
    branch: develop
    autoMerge: true
    templates:
      - geertvanhorrik
      - wildgums-components-public
```

This example will result in the following:

- CsvHelper => use geertvanhorrik
- Catel => use geertvanhorrik + catel (combined, so catel can override files)
- Orc.Controls => use geertvanhorrik + wildgums-components-public (combined, so wildgums-components-public can override files)

## Usage via code

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
