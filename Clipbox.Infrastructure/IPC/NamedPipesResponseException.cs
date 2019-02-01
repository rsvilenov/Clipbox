using System;

namespace Clipbox.Infrastructure.IPC
{
    public class NamedPipesResponseException : Exception
    {
        public NamedPipesResponseException()
        {
        }

        public NamedPipesResponseException(string message) 
            : base(message)
        {
        }

        public NamedPipesResponseException(string message, Exception inner)
                : base(message, inner)
        {
        }
    }
}
