using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.Extensibility.Contracts
{
    public abstract class IntermediateFormatBase : DisposableObject, IIntermediateFormat
    {
        private IIntermediateFormatMetadata _metadata;

        public IIntermediateFormatMetadata Metadata
        {
            get { return _metadata ?? (_metadata = DefineMetadata()); }
        }

        protected abstract IntermediateFormatMetadata DefineMetadata();

        protected class IntermediateFormatMetadata : IIntermediateFormatMetadata
        {
            public IntermediateFormatMetadata(string formatGuid, ClipboardContentClass contentClass, bool isTransferable)
            {
                FormatGuid = formatGuid;
                ContentClass = contentClass;
                IsTransferable = isTransferable;
            }

            public string FormatGuid { get; }

            public ClipboardContentClass ContentClass { get; }

            public bool IsTransferable { get; }
            
            public IIntermediateFormatMetadata ShallowClone()
            {
                return (IIntermediateFormatMetadata)MemberwiseClone();
            }
        }
    }
}
