using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class TransferableClipboardDataObject : 
        IHaveByteArray, IHaveClipboardFormat
    {
        /// <summary>
        /// <see cref="IHaveByteArray.ByteArray"/> 
        /// </summary>
        public byte[] ByteArray { get; internal set; }

        /// <summary>
        /// <see cref="IHaveClipboardFormat.ClipboardFormatName"/> 
        /// </summary>
        public string ClipboardFormatName { get; internal set; }
        
        public SerializableIntermediateFormatMetadata IntermediateFormatMetadata { get; internal set; }
    }
}
