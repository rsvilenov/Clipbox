using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats;
using System;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
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
