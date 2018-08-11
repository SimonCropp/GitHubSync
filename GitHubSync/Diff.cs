namespace GitHubSync
{
    public class Diff : MapperBase
    {
        internal Diff()
        { }

        internal void Add(Parts from, Parts to)
        {
            Add_Internal(from, to);
        }
    }
}