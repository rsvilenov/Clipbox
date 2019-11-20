using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    internal class ClipboardDataObject : IClipboardDataObject
    {
        internal ClipboardDataObject(string clipboardFormatName, IIntermediateFormat intermediateFormatObject)
        {
            IntermediateFormatObject = intermediateFormatObject;

            if (intermediateFormatObject.Metadata == null)
            {
                throw new ArgumentException($"The metadata for {intermediateFormatObject.GetType().Name} has not been set.");
            }
            IntermediateFormatMetadata = intermediateFormatObject.Metadata.ShallowClone();

            ClipboardFormatName = clipboardFormatName;
        }

        internal ClipboardDataObject(string clipboardFormatName, IIntermediateFormatMetadata intermediateFormatMetatada)
        {
            IntermediateFormatMetadata = intermediateFormatMetatada.ShallowClone();
            ClipboardFormatName = clipboardFormatName;
        }
        /// <summary>
        /// The container of the processed clipboard data.
        /// </summary>
        public IIntermediateFormat IntermediateFormatObject { get; internal set; }
        
        /// <summary>
        /// The metadata of the intermediate format.
        /// </summary>
        public IIntermediateFormatMetadata IntermediateFormatMetadata { get; internal set; }
        
        /// <summary>
        /// <see cref="IHaveClipboardFormat.ClipboardFormatName"/> 
        /// </summary>
        public string ClipboardFormatName { get; internal set; }
    }
}
