using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class TransferableClipboardDataObject : 
        IHaveByteArray, IHaveClipboardFormat, IHaveContentClass
    {
        /// <summary>
        /// <see cref="IHaveByteArray.ByteArray"/> 
        /// </summary>
        public byte[] ByteArray { get; internal set; }

        /// <summary>
        /// <see cref="IHaveClipboardFormat.ClipboardFormatName"/> 
        /// </summary>
        public string ClipboardFormatName { get; internal set; }

        /// <summary>
        /// <see cref="IHaveContentClass.ContentClass"/> 
        /// </summary>
        public ClipboardContentClass ContentClass { get; internal set; }

        public string IntermediateFormatGuid { get; internal set; }
    }
}
