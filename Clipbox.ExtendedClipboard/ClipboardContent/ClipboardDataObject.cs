using Clipbox.Core.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class ClipboardDataObject : IClipboardDataObject
    {
        /// <summary>
        /// <see cref="IClipboardDataObject.ByteArray"/> 
        /// </summary>
        public byte[] ByteArray { get; internal set; }

        /// <summary>
        /// <see cref="IClipboardDataObject.IsTransferable"/> 
        /// </summary>
        public bool IsTransferable { get; internal set; }
        
        [NonSerialized]
        private object _originalData;

        /// <summary>
        /// <see cref="IClipboardDataObject.OriginalData"/> 
        /// </summary>
        public object OriginalData
        {
            get { return _originalData; }
            internal set { _originalData = value; }
        }

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

        /// <summary>
        /// Don't allow this object to be constructed outside of our assembly
        /// </summary>
        internal ClipboardDataObject() { }
    }
}
