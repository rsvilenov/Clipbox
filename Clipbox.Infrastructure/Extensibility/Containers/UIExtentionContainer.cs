using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public class UIExtentionContainer :
        BaseExtentionContainer<UIExtention, WeakFactory>
    {
        public IEnumerable<UIExtention> ResolveAll()
        {
            foreach (Type t in base.RegisteredTypes)
            {
                yield return base.Resolve(t);
            }
        }
    }
}
