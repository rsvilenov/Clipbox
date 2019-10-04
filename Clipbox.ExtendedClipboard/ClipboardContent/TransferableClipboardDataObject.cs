using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class TransferableClipboardDataObject : IHaveByteArray, IHaveClipboardFormat, IHaveExtentionInfo
    {
        /// <summary>
        /// <see cref="IHaveByteArray.ByteArray"/> 
        /// </summary>
        public byte[] ByteArray { get; internal set; }

        /// <summary>
        /// <see cref="IHaveClipboardFormat.Format"/> 
        /// </summary>
        public string Format { get; internal set; }

        /// <summary>
        /// <see cref="IHaveClipboardFormat.ContentClass"/> 
        /// </summary>
        public ClipboardContentClass ContentClass { get; internal set; }

        public string FormatExtentionName { get; set; }

        public double FormatExtentionVersion { get; set; }
    }
}
