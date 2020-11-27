using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtension(
        description: "Support for copying/pasting of Dib V5 images",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    [ClipboardFormat("CF_DIBV5")]
    public class DibV5FormatSupport : ClipboardFormatSupport<DibV5IntermediateFormat>
    {
        private const int MAX_CONTENT_SIZE = 100 * 1024 * 1024;

        protected override DibV5IntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            if (hClipboardData == IntPtr.Zero)
            {
                throw new ArgumentNullException("hClipboardData");
            }

            int dataSize = MarshalExt.GetGlobalSize(hClipboardData);
            if (dataSize == 0)
            {
                throw new InvalidOperationException("GlobalSize reports 0 bytes");
            }

            if (dataSize > MAX_CONTENT_SIZE)
            {
                // too large
                return null;
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
                return new DibV5IntermediateFormat(data);
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

        protected override IntPtr GetPtr(DibV5IntermediateFormat intermediateFormat)
        {
            byte[] byteArray = intermediateFormat.GetDibV5Bytes();

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
    }
}
