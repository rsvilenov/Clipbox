using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    // TODO: rename these classes according to their contained types...
    public interface IClipboardFormatExtensionContainer
    {
        IClipboardFormatSupport ResolveClipboardFormatSupport(string format);
    }
}
