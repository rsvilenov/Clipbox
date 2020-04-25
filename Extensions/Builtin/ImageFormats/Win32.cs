using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.ImageFormats
{
    internal static class Win32
    {
        [DllImport("gdi32.dll")]
        internal static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteEnhMetaFile(IntPtr hemf);
        [DllImport("gdi32.dll")]
        internal static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer, byte[] lpbBuffer);
        [DllImport("gdi32.dll")]
        internal static extern IntPtr SetEnhMetaFileBits(uint cbBuffer, byte[] lpBuffer);

    }
}
