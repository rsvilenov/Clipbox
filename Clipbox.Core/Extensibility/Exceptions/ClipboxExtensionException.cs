using System;

namespace Clipbox.Core.Extensibility.Exceptions
{
    public class ClipboxExtensionException : ApplicationException
    {
        public Type ExtensionType { get; set; }

        public ClipboxExtensionException()
        {
        }

        public ClipboxExtensionException(string message)
        : base(message)
        {
        }

        public ClipboxExtensionException(string message, Type extensionType)
        : base(message)
        {
            ExtensionType = extensionType;
        }

        public ClipboxExtensionException(string message, Exception inner)
        : base(message, inner)
        {
        }

        public ClipboxExtensionException(string message, Type extensionType, Exception inner)
        : base(message, inner)
        {
            ExtensionType = extensionType;
        }
    }
}
