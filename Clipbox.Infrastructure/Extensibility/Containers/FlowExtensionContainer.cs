using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Infrastructure.Factories;
using System.Collections.Generic;
using System;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class FlowExtensionContainer :
        BaseExtensionContainer<FlowExtension, RegularFactory>
    {
        public IEnumerable<FlowExtension> ResolveAll()
        {
            foreach(Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
