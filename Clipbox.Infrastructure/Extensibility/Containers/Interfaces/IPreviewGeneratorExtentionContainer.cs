using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPreviewGeneratorExtentionContainer
    {
        IPreviewGenerator ResolveClipboardPreviewGenerator(string intermediateFormatGuid);

        KeyValuePair<string, IPreviewGenerator>? ResolveClipboardPreviewGeneratorWithHighestPriority(string[] formatGuids);
    }
}
