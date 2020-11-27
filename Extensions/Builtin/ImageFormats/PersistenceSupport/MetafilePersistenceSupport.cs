using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using System;
using System.Drawing.Imaging;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtension(
        description: "Persistence support for metafile images",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    [IntermediateFormatSupport("{69BD2743-DADE-4B24-B3A1-5B6D73BDAE32}")]
    public class MetafilePersistenceSupport : PersistenceSupportExtension<MetafileIntermediateFormat>
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
