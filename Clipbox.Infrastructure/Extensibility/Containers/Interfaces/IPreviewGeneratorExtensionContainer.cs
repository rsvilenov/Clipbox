using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPreviewGeneratorExtensionContainer
    {
        IPreviewGenerator ResolveClipboardPreviewGenerator(string intermediateFormatGuid);

        KeyValuePair<string, IPreviewGenerator>? ResolveClipboardPreviewGeneratorWithHighestPriority(string[] formatGuids);
    }
}
