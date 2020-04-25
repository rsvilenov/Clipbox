using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Win32;
using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "Unicode text formats", Version = 0.1)]
    [ClipboardFormat("CF_UNICODETEXT")]
    public class UnicodeTextFormatSupport : ClipboardFormatSupport<UnicodeTextIntermediateFormat>
    {
        protected override UnicodeTextIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);
            string str = Marshal.PtrToStringUni(hMemLocal);
            if (str != null)
            {
                // TODO: native strings are usually delimited by a zero byte,
                // but I should check if I need to remove it after PtrToStringUni
                str = str.TrimEnd(new char[] { '\0' });
            }

            return new UnicodeTextIntermediateFormat(str);
        }

        protected override IntPtr GetPtr(UnicodeTextIntermediateFormat intermediateFormat)
        {
            // thanks to kolibridev's Clippy (in github)
            // Marshal.StringToHGlobalUni allocates using LocalAlloc with LMEM_FIXED
            IntPtr hMemLocal = Marshal.StringToHGlobalUni(intermediateFormat.Text);
            try
            {
                int size = intermediateFormat.Text.Length * 2 + 1;
                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)size);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }
    }
}
