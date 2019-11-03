using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{

    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_METAFILEPICT")]
    public class MetafilePictFormatSupport : ClipboardFormatSupport<MetafilePictIntermediateFormat>
    {
        protected override MetafilePictIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hMetafilePict = Kernel32.GlobalLock(hClipboardData);

            try
            {
                var mfPict = (NativeStructures.METAFILEPICT)Marshal.PtrToStructure(hMetafilePict, typeof(NativeStructures.METAFILEPICT));

                uint metaSize = Gdi32.GetMetaFileBitsEx(mfPict.hMF, 0, IntPtr.Zero);
                IntPtr buffer = Marshal.AllocHGlobal((int)metaSize);
                Gdi32.GetMetaFileBitsEx(mfPict.hMF, metaSize, buffer);

                byte[] bits = new byte[metaSize];
                Marshal.Copy(buffer, bits, 0, (int)metaSize);

                var result = new MetafilePictIntermediateFormat
                (bits, 
                (NativeEnums.DeviceContextMapMode)mfPict.mm,
                mfPict.xExt,
                mfPict.yExt);

                return result;
            }
            finally
            {
                Kernel32.GlobalUnlock(hClipboardData);
            }
        }

        protected override IntPtr GetPtr(MetafilePictIntermediateFormat intermediateFormat)
        {
            // TODO: set the original metafile back to the intermediate format object,
            // because it may have become invalidated
            
            byte[] metafileBytes = intermediateFormat.MetafileBytes;

            // Create a metafile
            IntPtr hMetafile = Gdi32.SetMetaFileBitsEx((uint)metafileBytes.Length + (uint)Marshal.SizeOf<NativeStructures.METAFILEPICT>(), metafileBytes);


            var mfp = new NativeStructures.METAFILEPICT()
            {
                mm = (int)intermediateFormat.MappingMode,
                xExt = intermediateFormat.UnitsX,
                yExt = intermediateFormat.UnitsY,
                hMF = hMetafile
            };

            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(Marshal.SizeOf(typeof(NativeStructures.METAFILEPICT)),
                (hGlobalLocked) =>
                {
                    Marshal.StructureToPtr(mfp, hGlobalLocked, false);
                });

            return hGlobal;
        }

        public override void OnClipboardFailure(IntPtr hClipboardData)
        {
            IntPtr hMetafilePict = Kernel32.GlobalLock(hClipboardData);

            try
            {
                var mfPict = (NativeStructures.METAFILEPICT)Marshal.PtrToStructure(hMetafilePict, typeof(NativeStructures.METAFILEPICT));
                Gdi32.DeleteMetaFile(mfPict.hMF);

            }
            finally
            {
                // perform GlobalFree (this frees both locked and unlocked hGlobals)
                base.OnClipboardFailure(hClipboardData);
            }
        }
    }

}
