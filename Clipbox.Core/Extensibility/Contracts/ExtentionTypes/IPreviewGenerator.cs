using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public interface IPreviewGenerator : IClipboxExtension
    {
        Type PreviewDataType { get; }
    }
}
