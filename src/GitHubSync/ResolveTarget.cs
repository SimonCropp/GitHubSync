#nullable enable
namespace GitHubSync;

public delegate string ResolveTarget(string owner, string repository, string branch, string? path);