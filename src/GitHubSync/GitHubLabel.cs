using Octokit;

class GitHubLabel(Label label) : ILabel
{
    public string Name => label.Name;
}