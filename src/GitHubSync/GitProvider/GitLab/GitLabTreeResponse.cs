using NGitLab.Models;

class GitLabTreeResponse(string path, IEnumerable<Tree> tree) : ITreeResponse
{
    public string Path { get; } = path;
    public IReadOnlyList<ITreeItem> Tree { get; } = tree.Select(t => new GitLabTreeItem(t)).ToList<ITreeItem>();
}