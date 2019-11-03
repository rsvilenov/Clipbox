using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public abstract class SinglePreviewGenerator<TIntermediateFormat, TPreviewData> : IPreviewGenerator
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
