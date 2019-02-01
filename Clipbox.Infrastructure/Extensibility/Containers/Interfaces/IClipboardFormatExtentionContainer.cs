using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    // TODO: rename these classes according to their contained types...
    public interface IClipboardFormatExtentionContainer
    {
        IClipboardFormatSupport ResolveClipboardFormatSupport(string format);
    }
}
