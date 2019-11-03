using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IPreviewGenerator : IClipboxExtention
    {
        Type PreviewDataType { get; }
    }
}
