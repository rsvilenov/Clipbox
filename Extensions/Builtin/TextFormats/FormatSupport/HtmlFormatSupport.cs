using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Win32;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtension(
    description: "Copy/paste support for HTML data",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [ClipboardFormat("CF_HTML")]
    public class HtmlFormatSupport : ClipboardFormatSupport<TextIntermediateFormat>
    {
        private const int MAX_CONTENT_SIZE = 10 * 1024 * 1024;

        protected override TextIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            int contentSize = MarshalExt.GetGlobalSize(hClipboardData);
            if (contentSize > MAX_CONTENT_SIZE)
            {
                return null;
            }

            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData, contentSize);
            string str = Marshal.PtrToStringUni(hMemLocal);
            if (str != null)
            {
                // TODO: native strings are usually delimited by a zero byte,
                // but I should check if I need to remove it after PtrToStringUni
                str = str.TrimEnd(new char[] { '\0' });
            }

            return new TextIntermediateFormat(str);
        }

        protected override IntPtr GetPtr(TextIntermediateFormat intermediateFormat)
        {
            // thanks to kolibridev's Clippy (in github)
            // Marshal.StringToHGlobalUni allocates using LocalAlloc with LMEM_FIXED
            IntPtr hMemLocal = Marshal.StringToHGlobalUni(intermediateFormat.Text);
            try
            {
                int size = Encoding.UTF8.GetByteCount(intermediateFormat.Text) + 1;
                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)size);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }
    }
}
