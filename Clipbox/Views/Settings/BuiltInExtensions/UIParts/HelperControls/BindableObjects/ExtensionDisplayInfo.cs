using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.ObjectHelpers;
using Clipbox.Infrastructure.UI;
using System.Text.RegularExpressions;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls.BindableObjects
{
    public class ExtensionDisplayInfo : BindableObject
    {
        public PersistableExtensionInfo PersistableExtensionInfo { get; }

        public ExtensionDisplayInfo(PersistableExtensionInfo persistableExtensionInfo)
        {
            PersistableExtensionInfo = persistableExtensionInfo;
            ExtensionName = Regex.Replace(persistableExtensionInfo.TypeNameOfExtension, @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
            Version = persistableExtensionInfo.Version;
            Author = persistableExtensionInfo.Author;
            Description = persistableExtensionInfo.Description;
        }

        /// <summary>
        /// The name of the extension
        /// </summary>
        private string _extensionName;
        public string ExtensionName
        {
            get { return _extensionName; }
            set { SetField(ref _extensionName, value); }
        }

        /// <summary>
        /// The version of the extension
        /// </summary>
        private double _version;
        public double Version
        {
            get { return _version; }
            set { SetField(ref _version, value); }
        }

        /// <summary>
        /// Who wrote the extension
        /// </summary>
        private string _author;
        public string Author
        {
            get { return _author; }
            set { SetField(ref _author, value); }
        }

        /// <summary>
        /// The description of the extension
        /// </summary>
        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetField(ref _description, value); }
        }

        public override int GetHashCode()
        {
            return GetHashCodeHelper
                .Start
                .Append(ExtensionName)
                .Append(Description)
                .Append(Version)
                .Append(PersistableExtensionInfo.AssemblyLocation);
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == this.GetHashCode();
        }
    }
}
