public interface ICommit
{
    string Sha { get; }
    ITree Tree { get; }
}