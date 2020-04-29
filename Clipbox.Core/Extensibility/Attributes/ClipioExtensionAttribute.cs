using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Core.Extensibility.Attributes
{
    /// <summary>
    /// An attribute, indicating that a class is a Clipbox extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
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
                Version = extensionAttribute.Version,
                MinHostVersion = extensionAttribute.MinHostVersion
            };
        }

        private class ExtensionInfo : IExtensionInfo
        {
            public double Version { get; set; }

            public string Author { get; set; }

            public string Description { get; set; }

            public double MinHostVersion { get; set; }
        }
    }
}
