using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Exceptions;
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
        private readonly string _description;
        private readonly string _author;
        private readonly string _version;
        private readonly string _minHostVersion;


        /// <summary>
        /// Describes a Clipbox extension
        /// </summary>
        /// <param name="description">The description of the extension</param>
        /// <param name="author">Extension's author</param>
        /// <param name="version">Extension's version (ex: "1.4" - major.minor)</param>
        /// <param name="minHostVersion">The minimum version of Clipbox on which this extension can run (ex: "1.4" - major.minor)</param>
        public ClipboxExtensionAttribute(string description, string author, string version, string minHostVersion)
        {
            _description = description;
            _author = author;
            _version = version;
            _minHostVersion = minHostVersion;
        }

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

            string versionString = extensionAttribute.ConstructorArguments[2].Value as string;
            if (string.IsNullOrWhiteSpace(versionString))
            {
                throw new ClipboxExtensionException($"Please provide a valid value for {nameof(ClipboxExtensionAttribute)}'s constructor parameter 'version'.");
            }
        
            string minHostVersionString = extensionAttribute.ConstructorArguments[3].Value as string;
            if (string.IsNullOrWhiteSpace(minHostVersionString))
            {
                throw new ClipboxExtensionException($"Please provide a valid value for {nameof(ClipboxExtensionAttribute)}'s constructor parameter 'minHostVersion'.");
            }

            Version version = null;
            Version minHostVersion = null;

            try
            {
                version = new Version(versionString);
            }
            catch (FormatException ex)
            {
                throw new ClipboxExtensionException($"{versionString} is not a valid value for {nameof(ClipboxExtensionAttribute)}'s constructor parameter 'version'.", ex);
            }

            try
            {
                minHostVersion = new Version(minHostVersionString);
            }
            catch (FormatException ex)
            {
                throw new ClipboxExtensionException($"{minHostVersionString} is not a valid value for {nameof(ClipboxExtensionAttribute)}'s constructor parameter 'minHostVersion'.", ex);
            }

            return new ExtensionInfo
            {
                ExtensionObjectType = type,
                Description = extensionAttribute.ConstructorArguments[0].Value as string,
                Author = extensionAttribute.ConstructorArguments[1].Value as string,
                Version = version,
                MinHostVersion = minHostVersion
            };
        }

        private static CustomAttributeData GetCustomAttributeData(Type type)
        {
            return CustomAttributeData
                            .GetCustomAttributes(type)
                            .Where(cad => cad.AttributeType.AssemblyQualifiedName == typeof(ClipboxExtensionAttribute).AssemblyQualifiedName)
                            .FirstOrDefault();
        }

        private class ExtensionInfo : IExtensionInfo
        {
            public Type ExtensionObjectType { get; set; }

            public Version Version { get; set; }

            public string Author { get; set; }

            public string Description { get; set; }

            public Version MinHostVersion { get; set; }
        }
    }
}
