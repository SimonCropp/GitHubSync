# <img src="/src/icon.png" height="30px"> GitHubSync

[![Build status](https://ci.appveyor.com/api/projects/status/sjkccpx6avnw8vbv/branch/main?svg=true)](https://ci.appveyor.com/project/SimonCropp/GitHubSync)
[![NuGet Status](https://img.shields.io/nuget/v/GitHubSync.svg?label=GitHubSync)](https://www.nuget.org/packages/GitHubSync/)
[![NuGet Status](https://img.shields.io/nuget/v/GitHubSync.Tool.svg?label=dotnet%20tool)](https://www.nuget.org/packages/GitHubSync.Tool/)

A tool to help synchronizing specific files and folders across repositories

**See [Milestones](../../milestones?state=closed) for release notes.**


## .net API


### NuGet package

https://nuget.org/packages/GitHubSync/


### Usage

snippet: usage


## dotnet Tool

This tool allows reading the configuration from a file. This allows customization of the templates and repositories without
having to recompile any code.


### Installation

Ensure [dotnet CLI is installed](https://docs.microsoft.com/en-us/dotnet/core/tools/).

Install [GitHubSync.Tool](https://nuget.org/packages/GitHubSync.Tool/)

```ps
dotnet tool install -g GitHubSync.Tool
```


### Usage

You will need to define either a `Ockokit_OAuthToken` (GitHub) or `GitLab_OAuthToken` (GitLab) environment variable to log in to your git provider.

When using GitLab, you can optionally define a `GitLab_HostUrl` environment variable to specify the host of your GitLab instance. If not defined, the default value is `https://gitlab.com`.

Syncing between GitHub and GitLab is not currently supported.

Run against the current directory will use `githubsync.yaml` in the current directory:

```ps
githubsync
```

Run against a specific config file:

```ps
githubsync C:\Code\Project\sync.yaml
```


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

* CsvHelper => use geertvanhorrik
* Catel => use geertvanhorrik + catel (combined, so catel can override files)
* Orc.Controls => use geertvanhorrik + wildgums-components-public (combined, so wildgums-components-public can override files)


## Icon

[Sync](https://thenounproject.com/term/sync/290/) designed by [Edward Boatman](https://thenounproject.com/edward) from [The Noun Project](https://thenounproject.com).
