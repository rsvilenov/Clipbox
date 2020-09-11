using Clipbox.Core.Extensibility.Enums;
using System;

namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class AuthenticationStageEventArgs : EventArgs
    {
        public AuthenticationStageEventArgs(AuthenticationStage stage, string message = "")
        {
            Stage = stage;
            Message = message;
        }

        public AuthenticationStage Stage { get; }

        public string Message { get; }
    }
}
