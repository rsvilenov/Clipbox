using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SourceSessionInfo : ISessionInfo
    {
        public string SessionUniqueId { get; internal set; }

        public string UserName { get; internal set; }
    }
}
