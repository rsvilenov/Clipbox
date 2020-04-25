using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Win32;
using System;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "ANSI text formats", Version = 0.1)]
    [ClipboardFormat("Text")]
    public class AnsiTextFormatSupport : ClipboardFormatSupport<TextIntermediateFormat>
    {
        protected override TextIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);
            string str = Marshal.PtrToStringAnsi(hMemLocal);
            if (str != null)
            {
                // TODO: native strings are often delimited by a zero byte,
                // but check if we need to remove it after PtrToStringAnsi
                str = str.TrimEnd(new char[] { '\0' });
            }

            return new TextIntermediateFormat(str);
        }
        
        protected override IntPtr GetPtr(TextIntermediateFormat intermediateFormat)
        {
            // thanks to kolibridev's Clippy (in github)
            // Marshal.StringToHGlobalAnsi allocates using LocalAlloc with LMEM_FIXED
            IntPtr hMemLocal = Marshal.StringToHGlobalAnsi(intermediateFormat.Text);
            try
            {
                int size = intermediateFormat.Text.Length + 1;
                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)size);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }
    }
}
