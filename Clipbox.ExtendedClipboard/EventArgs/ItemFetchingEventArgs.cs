using Clipbox.Core.Extensibility.Contracts;
using System.ComponentModel;

namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ItemFetchingEventArgs : CancelEventArgs
    {
        public ItemFetchingEventArgs(IWindowInfo sourceWindowInfo)
        {
            SourceWindowInfo = sourceWindowInfo;
        }

        public IWindowInfo SourceWindowInfo { get; set; }
    }
}
