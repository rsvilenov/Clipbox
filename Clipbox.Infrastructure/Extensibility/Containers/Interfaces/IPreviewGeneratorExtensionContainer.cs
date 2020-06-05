using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPreviewGeneratorExtensionContainer
    {
        IEnumerable<Type> GetRegisteredTypes();

        IPreviewGenerator ResolveClipboardPreviewGenerator(string intermediateFormatGuid);

        IDictionary<string, IPreviewGenerator> ResolveClipboardPreviewGeneratorOrderedByPriority(string[] formatGuids);
    }
}
