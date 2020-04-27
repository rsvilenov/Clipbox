using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Win32;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "Html format", Version = 0.1)]
    [ClipboardFormat("CF_HTML")]
    public class HtmlFormatSupport : ClipboardFormatSupport<TextIntermediateFormat>
    {
        protected override TextIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);
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
