using System;

namespace Clipbox.Infrastructure.Extensibility.Loading
{
    public class ExtensionTypeInfo
    {
        public ExtensionTypeInfo(Type reflectionOnlyType, PersistableExtensionInfo persistableExtensionInfo)
        {
            ReflectionOnlyType = reflectionOnlyType;
            PersistableExtensionInfo = persistableExtensionInfo;
        }

        public Type ReflectionOnlyType { get; }

        public PersistableExtensionInfo PersistableExtensionInfo { get; }
    }
}
