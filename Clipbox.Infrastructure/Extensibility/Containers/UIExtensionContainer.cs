using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class UIExtensionContainer :
        BaseExtensionContainer<IFlowUIExtension, WeakFactory>
    {
        public IEnumerable<IFlowUIExtension> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
