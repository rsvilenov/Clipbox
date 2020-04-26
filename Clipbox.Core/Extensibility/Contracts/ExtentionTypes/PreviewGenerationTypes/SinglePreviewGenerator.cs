using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class SinglePreviewGenerator<TIntermediateFormat, TPreviewData> : 
        ConfigurableExtension, ISinglePreviewGenerator
        where TIntermediateFormat : IIntermediateFormat
        where TPreviewData : IPreviewData
    {
        public Type PreviewDataType
        {
            get
            {
                return typeof(TPreviewData);
            }
        }

        public IPreviewData GetPreview(IIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            return GetPreview((TIntermediateFormat)intermediateFormat, previewWidth, previewHeight);
        }

        protected abstract TPreviewData GetPreview(TIntermediateFormat intermediateFormat, int previewWidth, int previewHeight);
    }

    public interface ISinglePreviewGenerator : IPreviewGenerator
    {
        IPreviewData GetPreview(IIntermediateFormat intermediateFormat, int previewWidth, int previewHeight);
    }
}
