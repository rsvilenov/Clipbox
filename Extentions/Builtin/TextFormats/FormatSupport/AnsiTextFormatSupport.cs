using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Text;
using System;
using System.Runtime.InteropServices;
using Clipbox.Win32;
using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Extentions.BuiltIn.TextFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "ANSI text formats", Version = 0.1)]
    [ClipboardFormat("Text")]
    // the original Rtf is ANSI, and it seems that everything
    // that is sent to the clipboard as Rtf is ANSI
    [ClipboardFormat("Rich Text Format")]
    public class AnsiTextFormatSupport : ClipboardFormatSupport, IPersistableFormat
    {
        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is string))
            {
                throw new ArgumentException("data is not a string");
            }

            // thanks to kolibridev's Clippy (in github)
            // Marshal.StringToHGlobalAnsi allocates using LocalAlloc with LMEM_FIXED
            IntPtr hMemLocal = Marshal.StringToHGlobalAnsi((string)data);
            try
            {
                int size = ((string)data).Length + 1;
                return MarshalExt.HMemToHGlobalMemMovable(hMemLocal, (uint)size);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemLocal);
            }
        }
        
        public override object PtrToData(IntPtr hClipboardData)
        {
            IntPtr hMemLocal = MarshalExt.HGlobalMemMovableToHMem(hClipboardData);
            string str = Marshal.PtrToStringAnsi(hMemLocal);
            if (str != null)
            {
                // TODO: native strings are often delimited by a zero byte,
                // but check if we need to remove it after PtrToStringAnsi
                str = str.TrimEnd(new char[] { '\0' });
            }

            return str;
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            return Encoding.ASCII.GetString(byteArray);
        }

        public byte[] ObjectToByteArray(ref object data)
        {
            string str = data as string;

            if (str != null)
            {
                return Encoding.ASCII.GetBytes(str);
            }

            return null;
        }

        public bool RequiresSameMachine => false;

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Text;
    }
}
