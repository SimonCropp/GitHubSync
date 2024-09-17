using NGitLab.Models;

class GitLabOwner(User owner) : IOwner
{
    public string Login => owner.Username;
}