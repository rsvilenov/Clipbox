using System;

namespace Clipbox.Services.Exceptions
{
    class PreviewGenerationException : ApplicationException
    {
        public PreviewGenerationException()
        {
        }

        public PreviewGenerationException(string message)
            : base(message)
        {
        }

        public PreviewGenerationException(string message, Exception inner)
                : base(message, inner)
        {
        }
    }
}
