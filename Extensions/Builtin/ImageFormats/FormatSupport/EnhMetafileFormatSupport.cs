using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using System;
using System.Drawing.Imaging;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtension(
        description: "Support for copying/pasting of enhanced metafile images",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    [ClipboardFormat("CF_ENHMETAFILE_DataToPtr_Exception")]
   //// [ClipboardFormat("CF_METAFILEPICT")]
    public class EnhMetafileFormatSupport : ClipboardFormatSupport<MetafileIntermediateFormat>
    {
        protected override MetafileIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            // do not delete the handle, so that it can be used from other apps, accessing the same clipboard entry
            var mf = new Metafile(hClipboardData, false);
            //mf.Save(@"c:\test\mf.png");
            return new MetafileIntermediateFormat(mf);
        }

        protected override IntPtr GetPtr(MetafileIntermediateFormat intermediateFormat)
        {
            Metafile mf = intermediateFormat.GetCopyOfMetafile();
            IntPtr hEMF, hEMF2;

            // GetHenhmetafile() invalidates the mf, 
            // but this should be safe as MetafileIntermediateFormat returns a copy
            // of its internal metafile object.
            hEMF = mf.GetHenhmetafile();

            if (hEMF != IntPtr.Zero)
            {
                hEMF2 = Win32.CopyEnhMetaFile(hEMF, IntPtr.Zero);
                if (hEMF2 != IntPtr.Zero)
                {
                    return hEMF2;
                }
                Win32.DeleteEnhMetaFile(hEMF);
            }

            return IntPtr.Zero;
        }

        public override void OnClipboardFailure(IntPtr hClipboardData)
        {
            Win32.DeleteEnhMetaFile(hClipboardData);
        }
    }
}
