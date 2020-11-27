using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using System;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.PersistenceSupport
{
    [ClipboxExtension(
        description: "Persistence support for copied file paths",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
        )]
    [IntermediateFormatSupport("{3D2C252F-7838-49C2-9D5D-C6B4C92320D3}")]
    public class FilesystemPathsPersistenceSupport : PersistenceSupportExtension<FilesystemPathsIntermediateFormat>
    {
        protected override byte[] GetByteArray(FilesystemPathsIntermediateFormat intermediateFormat)
        {
            // convert the string array to hdrop string and then to bytearray
            string hdropString = Common.GetNullDelimitedString(intermediateFormat.Paths);
            return Encoding.Unicode.GetBytes(hdropString);
        }

        protected override FilesystemPathsIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            if (byteArray == null)
            {
                throw new ArgumentNullException("bytaArray");
            }

            string hdropString = Encoding.Unicode.GetString(byteArray);
            string[] filePaths = hdropString.Split(new string[] { "\0" }, StringSplitOptions.RemoveEmptyEntries);
            return new FilesystemPathsIntermediateFormat(filePaths);
        }
    }
}
