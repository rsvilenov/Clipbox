using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Enums;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class PersistableClipboardDataObject : IHaveByteArray, IHaveClipboardFormat, IHaveExtentionInfo
    {
        /// <summary>
        /// <see cref="IHaveByteArray.ByteArray"/> 
        /// </summary>
        public byte[] ByteArray { get; internal set; }

        /// <summary>
        /// Indicates whether the data can have any meaning on another computer.
        /// Filesystem paths, preferred drop effect and the like can't be transfered.
        /// </summary>
        public bool IsTransferable { get; internal set; }

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
