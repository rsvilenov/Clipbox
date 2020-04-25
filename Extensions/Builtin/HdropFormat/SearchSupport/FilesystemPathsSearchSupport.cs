using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.SearchSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{3D2C252F-7838-49C2-9D5D-C6B4C92320D3}")]
    public class FilesystemPathsSearchSupport : SearchSupportExtension<FilesystemPathsIntermediateFormat>
    {
        protected override string GetSearchTextData(FilesystemPathsIntermediateFormat intermediateFormat)
        {

            StringBuilder fileNames = new StringBuilder();

            foreach (string path in intermediateFormat.Paths)
            {
                // sometimes I get control characters instead of paths in the beginning of the hdrop string
                if (path.Any(cc => char.IsControl(cc)))
                {
                    continue;
                }

                try
                {
                    fileNames.AppendFormat(" {0} ", Path.GetFileNameWithoutExtension(path));
                }
                catch (ArgumentException)
                { }
            }

            return fileNames.ToString();
        }
    }
}
