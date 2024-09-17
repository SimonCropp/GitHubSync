using NGitLab.Models;

class GitLabRepository(Project project) : IRepository
{
    public IOwner Owner => new GitLabOwner(project.Owner);
    public string CloneUrl => project.HttpUrl;
}