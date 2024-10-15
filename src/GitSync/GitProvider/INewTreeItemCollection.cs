public interface INewTreeItemCollection
    : IList<INewTreeItem>
{
    void Add(string mode, string name, string sha, TreeType type);
}