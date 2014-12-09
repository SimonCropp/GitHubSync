namespace SyncOMatic
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable IgnoreWaitContext(this Task task)
        {
            return task.ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable<T> IgnoreWaitContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }
    }
}