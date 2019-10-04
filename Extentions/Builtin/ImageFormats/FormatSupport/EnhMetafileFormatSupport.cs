using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_ENHMETAFILE_DataToPtr_Exception")]
   //// [ClipboardFormat("CF_METAFILEPICT")]
    public class EnhMetafileFormatSupport : ClipboardFormatSupport, IPersistableFormat
    {

        [DllImport("gdi32.dll")]
        private static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteEnhMetaFile(IntPtr hemf);
        [DllImport("gdi32.dll")]
        private static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer, byte[] lpbBuffer);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SetEnhMetaFileBits(uint cbBuffer, byte[] lpBuffer);

        public override object PtrToData(IntPtr hClipboardData)
        {

            // do not delete the handle, so that it can be used from other apps, accessing the same clipboard entry
            var mf = new Metafile(hClipboardData, false);
            //mf.Save(@"c:\test\mf.png");
            return mf;
        }

        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is Metafile))
            {
                throw new ArgumentException("data should be metafile");
            }

            var mf = data as Metafile;
            IntPtr hEMF, hEMF2;

            data = mf.Clone();

            hEMF = mf.GetHenhmetafile(); // invalidates mf
            
            if (hEMF != IntPtr.Zero)
            {
                hEMF2 = CopyEnhMetaFile(hEMF, IntPtr.Zero);
                if (hEMF2 != IntPtr.Zero)
                {
                    return hEMF2;
                }
                DeleteEnhMetaFile(hEMF);
            }

            return IntPtr.Zero;
        }

        public bool RequiresSameMachine
        {
            get
            {
                return false;
            }
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            IntPtr hemf = SetEnhMetaFileBits((uint)byteArray.Length, byteArray);

            return new Metafile(hemf, true);
        }
        
        public byte[] ObjectToByteArray(ref object data)
        {
            var mf = data as Metafile;

            // the original data will get invalidated soon, 
            // so in order to prevent further crashes,
            // we need to replace it with a clone
            data = mf.Clone();

            IntPtr enhMetafileHandle = mf.GetHenhmetafile(); // invalidates the metafile
            if (enhMetafileHandle == IntPtr.Zero)
            {
                return null;
            }

            uint bufferSize = GetEnhMetaFileBits(enhMetafileHandle, 0, null);
            byte[] dataArray = new byte[bufferSize];
            GetEnhMetaFileBits(enhMetafileHandle, bufferSize, dataArray);
           
            DeleteEnhMetaFile(enhMetafileHandle);

            return dataArray;
        }

        public override void OnClipboardFailure(IntPtr hClipboardData)
        {
            DeleteEnhMetaFile(hClipboardData);
        }

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Image;
    }
}
