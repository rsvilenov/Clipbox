using Clipbox.Core.ClipboardContent.Enums;
using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IClipboardFormatSupport : 
        IClipboxExtention, IDisposable
    {
        ClipboardContentClass ContentClass { get; }
    }
}
