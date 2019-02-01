using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    /// <summary>
    /// TODO: rename this to PreviewGenerator
    /// </summary>
    /// <typeparam name="TPreviewData"></typeparam>
    public abstract class PreviewHandler<TPreviewData> : IPreviewHandler
        where TPreviewData : IPreviewData
    {
        public Type PreviewDataType
        {
            get
            {
                return typeof(TPreviewData);
            }
        }

        public abstract TPreviewData GetPreview(object data, int previewWidth, int previewHeight);
    }

    /// <summary>
    /// TODO: rename this to IPreviewGenerator
    /// </summary>
    public interface IPreviewHandler : IClipboxExtention
    {
        Type PreviewDataType { get; }
    }
}
