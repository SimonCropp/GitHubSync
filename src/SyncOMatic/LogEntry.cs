namespace SyncOMatic
{
    using System;

    public class LogEntry
    {
        internal LogEntry(string formatMessage, params object[] values)
        {
            At = DateTimeOffset.Now;
            What = string.Format(formatMessage, values);
        }

        public DateTimeOffset At { get; private set; }
        public string What { get; private set; }
    }
}
