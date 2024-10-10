using NGitLab.Models;

class GitLabCommit : ICommit
{
    GitLabCommit(string commitSha, string treeSha)
    {
        Sha = commitSha;
        Tree = new GitLabTree(treeSha);
    }

    public static async Task<GitLabCommit> CreateAsync(CommitInfo info, NGitLab.IRepositoryClient repositoryClient)
    {
        var commitSha = info.Id.ToString();

        var tree = repositoryClient
            .GetTreeAsync(new() { Ref = commitSha })
            .Aggregate(
                new GitLabNewTree(""),
                (nt, ct) =>
                {
                    nt.Tree.Add(ct.Mode, ct.Name, ct.Id.ToString(), ct.Type switch
                    {
                        ObjectType.blob => TreeType.Blob,
                        ObjectType.tree => TreeType.Tree,
                        _ => throw new InvalidOperationException()
                    });

                    return nt;
                });

        var treeSha = await GitHashHelper.GetTreeHash(tree);

        return new(commitSha, treeSha);
    }

    public string Sha { get; }
    public ITree Tree { get; }
}