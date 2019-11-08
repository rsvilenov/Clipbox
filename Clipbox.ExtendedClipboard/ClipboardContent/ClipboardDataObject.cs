using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    internal class ClipboardDataObject : IClipboardDataObject
    {

        internal ClipboardDataObject(string clipboardFormatName, IIntermediateFormat intermediateFormatObject)
        {
            IntermediateFormatObject = intermediateFormatObject;
            IntermediateFormatGuid = intermediateFormatObject.FormatGuid;
            ClipboardFormatName = clipboardFormatName;
        }

        internal ClipboardDataObject(string clipboardFormatName, string intermediateFormatGuid)
        {
            IntermediateFormatGuid = intermediateFormatGuid;
            ClipboardFormatName = clipboardFormatName;
        }
        /// <summary>
        /// The container of the processed clipboard data.
        /// </summary>
        public IIntermediateFormat IntermediateFormatObject { get; internal set; }
        
        /// <summary>
        /// <see cref="IHaveClipboardFormat.ClipboardFormatName"/> 
        /// </summary>
        public string ClipboardFormatName { get; internal set; }

        /// <summary>
        /// This diplicates <see cref="IIntermediateFormat.FormatGuid"/>
        /// but it is necessary for accessing intermediate format objects
        /// when they are "shrinked" (meaning: set to null). This is used
        /// only by the <see cref="PersistenceHelper"/> class.
        /// </summary>
        internal string IntermediateFormatGuid { get; }
    }
}
