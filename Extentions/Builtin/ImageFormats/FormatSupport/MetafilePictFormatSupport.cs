using Clipbox.Extentions.BuiltIn.ImageFormats.Types;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Runtime.InteropServices;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using Clipbox.Core.Enums;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{

    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_METAFILEPICT")]
    public class MetafilePictFormatSupport : ClipboardFormatSupport, IPersistableFormat
    {
        public bool RequiresSameMachine
        {
            get
            {
                return false;
            }
        }

        public byte[] ObjectToByteArray(ref object data)
        {
            return null;
        }



        public object ByteArrayToObject(byte[] byteArray)
        {
            return null;
        }
        

        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is MetafilePictContainer))
            {
                throw new ArgumentException("data should be MetafileContainer");
            }

            MetafilePictContainer mfContainer = (MetafilePictContainer)data;
            byte[] metafileBits = mfContainer.MetafileBits;

            // Create a metafile
            IntPtr hMetafile = Gdi32.SetMetaFileBitsEx((uint)metafileBits.Length +(uint) Marshal.SizeOf<NativeStructures.METAFILEPICT>(), metafileBits);


            var mfp = new NativeStructures.METAFILEPICT()
            {
                mm = (int)mfContainer.MappingMode,
                xExt = mfContainer.UnitsX,
                yExt = mfContainer.UnitsY,
                hMF = hMetafile
            };

            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(Marshal.SizeOf(typeof(NativeStructures.METAFILEPICT)), 
                (hGlobalLocked) =>
            {
                Marshal.StructureToPtr(mfp, hGlobalLocked, false);
            });

            return hGlobal;
        }

        public override object PtrToData(IntPtr hClipboardData)
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

                var result = new MetafilePictContainer
                {
                    MetafileBits = bits,
                    MappingMode = (NativeEnums.DeviceContextMapMode)mfPict.mm,
                    UnitsX = mfPict.xExt,
                    UnitsY = mfPict.yExt
                };

                return result;
            }
            finally
            {
                Kernel32.GlobalUnlock(hClipboardData);
            }
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

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Image;

    }

}
