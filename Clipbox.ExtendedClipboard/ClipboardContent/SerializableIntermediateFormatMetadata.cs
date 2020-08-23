using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SerializableIntermediateFormatMetadata : IIntermediateFormatMetadata
    {
        public SerializableIntermediateFormatMetadata() { }

        public SerializableIntermediateFormatMetadata(IIntermediateFormatMetadata originalMetadata)
        {
            FormatGuid = originalMetadata.FormatGuid;
            ContentClass = originalMetadata.ContentClass;
            IsTransferable = originalMetadata.IsTransferable;
            IsAuxiliary = originalMetadata.IsAuxiliary;
        }

        public string FormatGuid { get; set; }

        public ClipboardContentClass ContentClass { get; set; }

        public bool IsTransferable { get; set; }

        public bool IsAuxiliary { get; set; }

        public IIntermediateFormatMetadata ShallowClone()
        {
            return (IIntermediateFormatMetadata)MemberwiseClone();
        }
    }
}
