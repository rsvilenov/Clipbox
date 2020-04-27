using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPreviewGeneratorExtensionContainer
    {
        IEnumerable<Type> GetRegisteredTypes();

        IPreviewGenerator ResolveClipboardPreviewGenerator(string intermediateFormatGuid);

        KeyValuePair<string, IPreviewGenerator>? ResolveClipboardPreviewGeneratorWithHighestPriority(string[] formatGuids);
    }
}
