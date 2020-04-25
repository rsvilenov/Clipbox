using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class UIExtensionContainer :
        BaseExtensionContainer<UIExtension, WeakFactory>
    {
        public IEnumerable<UIExtension> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
