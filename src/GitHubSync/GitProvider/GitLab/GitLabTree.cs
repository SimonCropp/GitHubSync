class GitLabTree(string sha) : ITree
{
    public string Sha { get; } = sha;
}