using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class SettingsUIExtensionContainer :
        BaseExtensionContainer<SettingsUIExtension, RegularFactory>
    {
        public IEnumerable<SettingsUIExtension> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
