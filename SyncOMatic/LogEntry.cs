using System;

namespace SyncOMatic
{
    public class LogEntry
    {
        internal LogEntry(string formatMessage, params object[] values)
        {
            At = DateTimeOffset.Now;
            What = string.Format(formatMessage, values);
        }

        public DateTimeOffset At { get; }
        public string What { get; }
    }
}
