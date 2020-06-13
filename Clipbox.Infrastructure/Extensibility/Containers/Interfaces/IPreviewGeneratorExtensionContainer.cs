using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers.Interfaces
{
    public interface IPreviewGeneratorExtensionContainer
    {
        IEnumerable<Type> GetRegisteredTypes();

        IPreviewGenerator Resolve(string intermediateFormatGuid);

        IDictionary<string, IPreviewGenerator> ResolveOrderedByPriorityDesc(string[] formatGuids);
    }
}
