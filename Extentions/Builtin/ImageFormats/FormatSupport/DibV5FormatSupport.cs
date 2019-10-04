using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_DIBV5")]
    public class DibV5FormatSupport
        : ClipboardFormatSupport, IPersistableFormat
    {
        public bool RequiresSameMachine
        {
            get
            {
                return false;
            }
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            return byteArray;
        }

        public byte[] ObjectToByteArray(ref object data)
        {
            if (!(data is byte[]))
            {
                throw new ArgumentException("data should be byte[]");
            }

            return data as byte[];
        }

        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is byte[]))
            {
                throw new ArgumentException("data should be byte[]");
            }
            byte[] byteArray = (byte[])data;

            IntPtr hMemLocal = Marshal.AllocHGlobal(byteArray.Length);

            try
            {
                Marshal.Copy(byteArray, 0, hMemLocal, byteArray.Length);

                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)byteArray.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }

        public override object PtrToData(IntPtr hClipboardData)
        {
            if (hClipboardData == IntPtr.Zero)
            {
                throw new ArgumentNullException("hClipboardData");
            }

            int dataSize = Kernel32.GlobalSize(hClipboardData);
            if (dataSize == 0)
            {
                throw new InvalidOperationException("GlobalSize reports 0 bytes");
            }

            //IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);

            IntPtr source = Kernel32.GlobalLock(hClipboardData);
            if (source == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                byte[] data = new byte[dataSize];
                Marshal.Copy(source, data, 0, dataSize);
                return data;
            }
            finally
            {
                Kernel32.GlobalUnlock(source);
            }

            //if (hMemLocal == null)
            //{
            //    throw new InvalidOperationException("HGlobalMemMovableToHMem failure.");
            //}

            //try
            //{
                
            //}
            //finally
            //{
            //    Marshal.FreeHGlobal(hMemLocal);
            //}
        }

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Image;
    }
}
