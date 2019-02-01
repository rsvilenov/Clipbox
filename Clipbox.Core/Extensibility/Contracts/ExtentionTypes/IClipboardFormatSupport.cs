using Clipbox.Core.Enums;
using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IClipboardFormatSupport : 
        IClipboxExtention, IDisposable
    {
        ClipboardContentClass ContentClass { get; }
    }
}
