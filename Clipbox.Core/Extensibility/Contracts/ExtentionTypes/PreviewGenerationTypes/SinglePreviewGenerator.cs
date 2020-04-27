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

        public IPreviewData GetPreview(IIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            return GetPreview((TIntermediateFormat)intermediateFormat, previewParams);
        }

        protected abstract TPreviewData GetPreview(TIntermediateFormat intermediateFormat, PreviewParams previewParams);
    }

    public interface ISinglePreviewGenerator : IPreviewGenerator
    {
        IPreviewData GetPreview(IIntermediateFormat intermediateFormat, PreviewParams previewParams);
    }
}
