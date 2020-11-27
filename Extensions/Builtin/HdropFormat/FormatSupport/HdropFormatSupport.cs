using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FormatSupport
{
    [ClipboxExtension(
         description: "Copying/psating of file(s) from Windows Explorer",
         author: ProductConstants.PRODUCT_NAME,
         version: "1.0",
         minHostVersion: "1.0"
         )]
    [ClipboardFormat("CF_HDROP")]
    public class HdropFormatSupport : ClipboardFormatSupport<FilesystemPathsIntermediateFormat>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DROPFILES
        {
            public int pFiles;
            public Point pt;
            public bool fNC;
            public bool fWide;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch);
        
        protected override FilesystemPathsIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            IntPtr hDrop = Kernel32.GlobalLock(hClipboardData);

            List<string> pathList = new List<string>();

            try
            {
                // https://msdn.microsoft.com/en-us/library/windows/desktop/bb776408(v=vs.85).aspx

                // get the file number
                int fileNum = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);

                if (fileNum == 0)
                {
                    // the docs say that 0 means error, but fail to specify whether we can get the 
                    // error by calling GetLastError, so we will just assume that
                    MarshalExt.ThrowOnWin32Error();
                }

                if (fileNum > 0)
                {
                    for (uint i = 0; i < fileNum; i++)
                    {
                        // get the length of the filename (this is the number of characters only - the null terminator is not included)
                        int pathLength = DragQueryFile(hDrop, i, null, 0);
                        if (pathLength == 0)
                        {
                            MarshalExt.ThrowOnWin32Error();
                        }

                        // the filename is a null terminated string, so we need to increase the length of the buffer by 1.
                        // DragQueryFile works with number of characters, not with bytes, so the encoding doesn't matter here
                        pathLength += 1;

                        // allocate a buffer
                        StringBuilder lpszFile = new StringBuilder(pathLength);

                        // get the actual filename
                        int success = DragQueryFile(hDrop, i, lpszFile, lpszFile.Capacity);
                        if (success == 0)
                        {
                            MarshalExt.ThrowOnWin32Error();
                        }

                        if (lpszFile.Length > 0)
                        {
                            string path = lpszFile.ToString();

                            // sometimes I get control characters instead of paths in the beginning of the hdrop string
                            if (path.Any(cc => char.IsControl(cc)))
                            {
                                continue;
                            }

                            pathList.Add(path);
                        }
                    }
                }
            }
            finally
            {
                Kernel32.GlobalUnlock(hDrop);
            }

            return new FilesystemPathsIntermediateFormat(pathList);
        }

        protected override IntPtr GetPtr(FilesystemPathsIntermediateFormat intermediateFormat)
        {
            string pathsString = Common.GetNullDelimitedString(intermediateFormat.Paths);
            var pathsBytes = Encoding.Unicode.GetBytes(pathsString);

            DROPFILES structure = new DROPFILES();
            structure.pFiles = Marshal.SizeOf(structure);
            structure.fWide = true;


            int totalSize = Marshal.SizeOf(structure) + pathsBytes.Length;


            IntPtr hGlobal = MarshalExt.CreateHGlobalMovable(totalSize, (hGlobalLocked) =>
            {
                Marshal.StructureToPtr(structure, hGlobalLocked, false);
                IntPtr pathsPtr = new IntPtr(hGlobalLocked.ToInt64() + Marshal.SizeOf(structure));

                Marshal.Copy(pathsBytes, 0, pathsPtr, pathsBytes.Length);
            });

            return hGlobal;
        }
    }
}
