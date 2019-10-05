using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Services.DataObjects;
using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class PreviewGenerationEventArgs : EventArgs
    {
        public PreviewDataTransferObject PreviewDataTransferObject { get; }

        public IClipboardItem ClipboardItem { get; }

        public PreviewGenerationEventArgs(IClipboardItem clipboardItem, PreviewDataTransferObject pdto)
        {
            PreviewDataTransferObject = pdto;
            ClipboardItem = clipboardItem;
        }
    }
}
