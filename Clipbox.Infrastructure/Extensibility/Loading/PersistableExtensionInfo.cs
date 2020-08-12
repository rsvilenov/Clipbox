using System;

namespace Clipbox.Infrastructure.Extensibility.Loading
{
    [Serializable]
    public class PersistableExtensionInfo
    {
        /// <summary>
        /// The string representation of the full info for System.Type of the extension
        /// </summary>
        public string ExtensionObjectTypeFullName { get; set; }

        /// <summary>
        /// The string representation of the System.Type of the extension
        /// </summary>
        public string TypeNameOfExtension { get; set; }

        /// <summary>
        /// The full name of the assembly
        /// </summary>
        public string AssemblyFullName { get; set; }

        /// <summary>
        /// The full file path of the assembly
        /// </summary>
        public string AssemblyLocation { get; set; }

        /// <summary>
        /// The version of the extension
        /// </summary>
        public double Version { get; set; }

        /// <summary>
        /// Who wrote the extension
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The description of the extension
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The minimum version of Clipbox on which this extension can run
        /// </summary>
        public double MinHostVersion { get; set; }
    }
}
