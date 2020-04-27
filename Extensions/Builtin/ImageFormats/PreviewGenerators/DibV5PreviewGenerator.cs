using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{CFCA3819-C2EF-4F94-B9B7-49D402CA0652}")]
    public class DibV5PreviewGenerator : SinglePreviewGenerator<DibV5IntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(DibV5IntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            byte[] byteArray = intermediateFormat.GetDibV5Bytes();
            int size = byteArray.Length;

            var hBitmap = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
            try
            {
                // https://stackoverflow.com/questions/12101146/how-can-i-convert-a-cf-dibv5-to-a-bitmapsource
                IntPtr scan0 = IntPtr.Zero;
                var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap.AddrOfPinnedObject(), typeof(BITMAPV5HEADER));

                //int stride = (int)(bmi.bV5SizeImage / bmi.bV5Height);
                //long offset = bmi.bV5Size + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>();
                //if (bmi.bV5Compression == (uint)BitmapCompressionMode.BI_BITFIELDS)
                //{
                //    offset += 12; //bit masks follow the header
                //}
                //scan0 = new IntPtr(hBitmap.AddrOfPinnedObject().ToInt64() + offset);

                // try to guess the pixel format
                var pixelFormat = PixelFormat.Format32bppArgb;
                switch (bmi.bV5BitCount)
                {
                    case 16:
                        pixelFormat = PixelFormat.Format16bppRgb555;
                        break;
                    case 24:
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    case 32:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        throw new FormatException("The format is not supported");
                }

                // https://stackoverflow.com/questions/14333336/how-can-i-convert-a-cf-dibv5-from-clipboard-format17-to-a-transparent-bitmap/14335591#14335591
                using (Bitmap bitmap = new Bitmap((int)bmi.bV5Width, (int)bmi.bV5Height, -
                           (int)(bmi.bV5SizeImage / bmi.bV5Height), pixelFormat,
                           new IntPtr(hBitmap.AddrOfPinnedObject().ToInt32()
                           + bmi.bV5Size + (bmi.bV5Height - 1)
                           * (int)(bmi.bV5SizeImage / bmi.bV5Height))))
                {
                    //Bitmap bitmap = new Bitmap(bmi.bV5Width, bmi.bV5Height, -stride, pixelFormat, scan0);
                    return new ImagePreviewData
                    {
                        Preview = bitmap.GetScaled(previewParams.PreviewHeight * 2, previewParams.PreviewHeight),
                        Annotation = $"{bitmap.Width} x {bitmap.Height}"
                    };
                }
            }
            finally
            {
                hBitmap.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPV5HEADER
        {
            /// <summary>
            /// DIB Header Size
            /// </summary>
            public uint bV5Size;

            /// <summary>
            /// Image Width (W)
            /// </summary>
            public int bV5Width;

            /// <summary>
            /// Image Height (H)
            /// </summary>
            public int bV5Height;

            /// <summary>
            /// Planes
            /// </summary>
            public UInt16 bV5Planes;

            /// <summary>
            /// Bits per Pixel
            /// </summary>
            public UInt16 bV5BitCount;

            /// <summary>
            /// Compression
            /// </summary>
            public uint bV5Compression;

            /// <summary>
            /// Image Size
            /// </summary>
            public uint bV5SizeImage;

            /// <summary>
            /// X Pixels per Meter
            /// </summary>
            public int bV5XPelsPerMeter;

            /// <summary>
            /// Y Pixels per Meter
            /// </summary>
            public int bV5YPelsPerMeter;

            /// <summary>
            /// Colors in Color Table
            /// </summary>
            public UInt16 bV5ClrUsed;

            /// <summary>
            /// Important Colors Count
            /// </summary>
            public UInt16 bV5ClrImportant;

            /// <summary>
            /// Red channel bitmask
            /// </summary>
            public UInt16 bV5RedMask;

            /// <summary>
            /// Green channel bitmask
            /// </summary>
            public UInt16 bV5GreenMask;

            /// <summary>
            /// Blue channel bitmask
            /// </summary>
            public UInt16 bV5BlueMask;

            /// <summary>
            /// Alpha channel bitmask
            /// </summary>
            public UInt16 bV5AlphaMask;

            /// <summary>
            /// Color space type
            /// </summary>
            public UInt16 bV5CSType;

            /// <summary>
            /// Color space endpoints
            /// </summary>
            public IntPtr bV5Endpoints;

            /// <summary>
            /// Gamma for Red channel
            /// </summary>
            public UInt16 bV5GammaRed;

            /// <summary>
            /// Gamma for Green channel
            /// </summary>
            public UInt16 bV5GammaGreen;

            /// <summary>
            /// Gamma for blue channel
            /// </summary>
            public UInt16 bV5GammaBlue;

            /// <summary>
            /// Intent
            /// </summary>
            public UInt16 bV5Intent;

            /// <summary>
            /// ICC Profile Data
            /// </summary>
            public UInt16 bV5ProfileData;

            /// <summary>
            /// ICC Profile Size
            /// </summary>
            public UInt16 bV5ProfileSize;

            /// <summary>
            /// Reserved
            /// </summary>
            public UInt16 bV5Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }


        public enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }
    }
}
