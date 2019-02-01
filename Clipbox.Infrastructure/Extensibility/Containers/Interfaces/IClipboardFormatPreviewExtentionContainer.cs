using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IClipboardFormatPreviewExtentionContainer
    {
        IPreviewHandler ResolveClipboardPreviewHandler(string format);
    }
}
