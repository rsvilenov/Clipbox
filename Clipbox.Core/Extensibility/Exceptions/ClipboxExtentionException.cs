using System;

namespace Clipbox.Core.Extensibility.Exceptions
{
    public class ClipboxExtentionException : ApplicationException
    {
        public Type ExtentionType { get; set; }

        public ClipboxExtentionException()
        {
        }

        public ClipboxExtentionException(string message)
        : base(message)
        {
        }

        public ClipboxExtentionException(string message, Type extentionType)
        : base(message)
        {
            ExtentionType = extentionType;
        }

        public ClipboxExtentionException(string message, Exception inner)
        : base(message, inner)
        {
        }

        public ClipboxExtentionException(string message, Type extentionType, Exception inner)
        : base(message, inner)
        {
            ExtentionType = extentionType;
        }
    }
}
