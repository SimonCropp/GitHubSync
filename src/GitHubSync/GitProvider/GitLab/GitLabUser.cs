using NGitLab.Models;

class GitLabUser(Session user) : IUser
{
    public string Name => user.Name;
    public string? Email => user.Email;
}