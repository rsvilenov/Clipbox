using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;
using System;
using System.Drawing.Imaging;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{69BD2743-DADE-4B24-B3A1-5B6D73BDAE32}")]
    public class FilesystemPathsPersistenceSupport : PersistenceSupportExtension<MetafileIntermediateFormat>
    {
        protected override byte[] GetByteArray(MetafileIntermediateFormat intermediateFormat)
        {
            var mf = intermediateFormat.GetCopyOfMetafile();
            
            IntPtr enhMetafileHandle = mf.GetHenhmetafile(); // invalidates the metafile
            if (enhMetafileHandle == IntPtr.Zero)
            {
                return null;
            }

            uint bufferSize = Win32.GetEnhMetaFileBits(enhMetafileHandle, 0, null);
            byte[] dataArray = new byte[bufferSize];
            Win32.GetEnhMetaFileBits(enhMetafileHandle, bufferSize, dataArray);

            Win32.DeleteEnhMetaFile(enhMetafileHandle);

            return dataArray;
        }

        protected override MetafileIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            IntPtr hemf = Win32.SetEnhMetaFileBits((uint)byteArray.Length, byteArray);
            Metafile mf = new Metafile(hemf, true);
            return new MetafileIntermediateFormat(mf);
        }
    }
}
