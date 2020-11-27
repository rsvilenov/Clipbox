using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    /// <summary>
    /// Represents the metadata for an extension
    /// </summary>
    public interface IExtensionInfo
    {
        /// <summary>
        /// The System.Type of the extension
        /// </summary>
        Type ExtensionObjectType { get; }

        /// <summary>
        /// The version of the extension
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Who wrote the extension
        /// </summary>
        string Author { get; }

        /// <summary>
        /// The description of the extension
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The minimum version of Clipbox on which this extension can run
        /// </summary>
        Version MinHostVersion { get; }
    }
}
