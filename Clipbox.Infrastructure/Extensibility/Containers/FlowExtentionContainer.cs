using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Factories;
using System.Collections.Generic;
using System;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class FlowExtentionContainer :
        BaseExtentionContainer<FlowExtention, RegularFactory>
    {
        public IEnumerable<FlowExtention> ResolveAll()
        {
            foreach(Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
