using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;
using System.Reflection;

namespace Clipbox.Core.Extensibility.Attributes
{
    /// <summary>
    /// An attribute, indicating that a class is a Clipbox extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
    public class ClipboxExtensionAttribute : Attribute
    {
        /// <summary>
        /// The version of the extension
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Who wrote the extension
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The version of the extension
        /// </summary>
        public double Version { get; set; }

        /// <summary>
        /// The minimum version of Clipbox on which this extension can run
        /// </summary>
        public double MinHostVersion { get; set; }

        public static bool IsDefinedFor(Type type)
        {
            return GetCustomAttributeData(type) != null;
        }

        public static IExtensionInfo GetExtensionInfo(Type type)
        {
            CustomAttributeData extensionAttribute = GetCustomAttributeData(type);

            if (extensionAttribute == null)
            {
                throw new InvalidOperationException($"Attribute {nameof(ClipboxExtensionAttribute)} has not been defined for type {type.Name}");
            }

            return new ExtensionInfo
            {
                ExtensionObjectType = type,
                Author = GetNamedArgumentValue<string>(extensionAttribute, nameof(Author)),
                Description = GetNamedArgumentValue<string>(extensionAttribute, nameof(Description)),
                Version = GetNamedArgumentValue<double>(extensionAttribute, nameof(Version)),
                MinHostVersion = GetNamedArgumentValue<double>(extensionAttribute, nameof(MinHostVersion))
            };
        }

        private static CustomAttributeData GetCustomAttributeData(Type type)
        {
            return CustomAttributeData
                            .GetCustomAttributes(type)
                            .Where(cad => cad.AttributeType.AssemblyQualifiedName == typeof(ClipboxExtensionAttribute).AssemblyQualifiedName)
                            .FirstOrDefault();
        }

        private static T GetNamedArgumentValue<T>(CustomAttributeData attrData, string argumentName)
        {
            foreach (CustomAttributeNamedArgument arg in attrData.NamedArguments)
            {
                if (arg.MemberInfo?.Name == argumentName && arg.MemberInfo.ReflectedType == typeof(T))
                {
                    return (T)arg.TypedValue.Value;
                }
            }

            return default(T);
        }

        private class ExtensionInfo : IExtensionInfo
        {
            public Type ExtensionObjectType { get; set; }

            public double Version { get; set; }

            public string Author { get; set; }

            public string Description { get; set; }

            public double MinHostVersion { get; set; }
        }
    }
}
