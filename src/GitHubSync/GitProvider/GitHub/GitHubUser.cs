using Octokit;

class GitHubUser(User user) : IUser
{
    public string Name => user.Name;
    public string? Email => user.Email;
}