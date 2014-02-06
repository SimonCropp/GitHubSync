namespace SyncOMatic.Core
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class MissingSourceException : Exception
    {
        public MissingSourceException()
        {
        }

        public MissingSourceException(string message)
            : base(message)
        {
        }

        public MissingSourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MissingSourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
