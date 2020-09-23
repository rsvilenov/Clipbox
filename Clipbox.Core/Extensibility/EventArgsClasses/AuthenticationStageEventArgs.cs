using Clipbox.Core.Extensibility.Enums;
using System;

namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class AuthenticationStageEventArgs : EventArgs
    {
        public AuthenticationStageEventArgs(AuthenticationStage stage, string message = "", string details = "")
        {
            Stage = stage;
            Message = message;
            Details = details;
        }

        public AuthenticationStage Stage { get; }

        public string Message { get; }

        public string Details { get; }
    }
}
