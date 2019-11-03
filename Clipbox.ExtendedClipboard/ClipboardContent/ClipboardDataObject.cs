using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    internal class ClipboardDataObject : IClipboardDataObject
    {
        /// <summary>
        /// The container of the processed clipboard data.
        /// </summary>
        public IIntermediateFormat IntermediateFormatObject { get; internal set; }
        
        /// <summary>
        /// <see cref="IHaveClipboardFormat.ClipboardFormatName"/> 
        /// </summary>
        public string ClipboardFormatName { get; internal set; }
    }
}
