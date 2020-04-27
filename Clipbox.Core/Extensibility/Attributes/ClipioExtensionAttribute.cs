using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ClipboxExtensionAttribute : Attribute
    {
        public string Description { get; set; }

        public string Author { get; set; }

        public double Version { get; set; }

        public static bool IsDefinedFor(Type type)
        {
            var formatAttributes = (ClipboxExtensionAttribute[])type.GetCustomAttributes(typeof(ClipboxExtensionAttribute), true);
            return formatAttributes.Any();
        }

        public static IExtensionInfo GetExtensionInfo(Type type)
        {
            var extensionAttribute = (ClipboxExtensionAttribute)type.
                GetCustomAttributes(typeof(ClipboxExtensionAttribute), true).
                FirstOrDefault();

            if (extensionAttribute == null)
            {
                throw new InvalidOperationException($"Attribute {nameof(ClipboxExtensionAttribute)} has not been defined for type {type.Name}");
            }

            return new ExtensionInfo
            {
                Author = extensionAttribute.Author,
                Description = extensionAttribute.Description,
                Version = extensionAttribute.Version
            };
        }

        private class ExtensionInfo : IExtensionInfo
        {
            public double Version { get; set; }

            public string Author { get; set; }

            public string Description { get; set; }
        }
    }
}
