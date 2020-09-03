using System;

namespace Clipbox.Services.Exceptions
{
    public class CloudSyncException : ApplicationException
    {
        public CloudSyncException()
        {
        }

        public CloudSyncException(string message)
            : base(message)
        {
        }

        public CloudSyncException(string message, Exception inner)
                : base(message, inner)
        {
        }
    }
}
