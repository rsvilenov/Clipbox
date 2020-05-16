using Clipbox.Core.Extensibility.Contracts;
using System.Collections.Generic;
using System.ComponentModel;

namespace Clipbox.ExtendedClipboard.EventArgs
{
    public class ItemStoringEventArgs : CancelEventArgs
    {
        public ItemStoringEventArgs(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> intermediateFormats)
        {
            SourceWindowInfo = sourceWindowInfo;
            IntermediateFormats = intermediateFormats;
        }

        public IWindowInfo SourceWindowInfo { get; private set; }

        public IEnumerable<IIntermediateFormat> IntermediateFormats { get; set; }

        public bool AreIntermediateFormatsChanged { get; set; }
    }
}
