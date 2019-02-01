using Clipbox.Extentions.BuiltIn.ImageFormats.Types;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Win32.Native;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.PreviewHandlers
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_METAFILEPICT")]
    public class MetafilePictPreviewHandler : PreviewHandler<ImagePreviewData>
    {
        public override ImagePreviewData GetPreview(object data, int previewWidth, int previewHeight)
        {
            var metafilePict = data as MetafilePictContainer;
            if (metafilePict == null)
            {
                throw new ArgumentException("data is not MetafileContainer");
            }

            // TODO: support different mapping modes
            if (metafilePict.MappingMode != NativeEnums.DeviceContextMapMode.HiMetric)
            {
                Debug.WriteLine($"Mapping mode '{metafilePict.MappingMode}' will be ignored. 'HiMetric' will be used instead, so the final image's dimentions may be wrong.");
            }

            // Convert the old metafile format to enhanced metafile
            IntPtr enhMetafilePtr = Gdi32.SetWinMetaFileBits((uint)metafilePict.MetafileBits.Length, metafilePict.MetafileBits, IntPtr.Zero, IntPtr.Zero);

            var previewData = new ImagePreviewData();

            // convert the HiMetric to pixels
            int width = HiMetricToPx(metafilePict.UnitsX);
            int height = HiMetricToPx(metafilePict.UnitsY);

            // Create a Metafile instance and delete the handle
            using (Image img = new Metafile(enhMetafilePtr, true /* 'true' deletes the hMetafile */))
            {
                // I haven't found a way to set the metafile's dimentions upon its creation, so I will
                // copy the newly created image (which has fullHD dimentions when I test the code) to
                // an empty bitmap with the rigth width and height
                using (Image bmp = new Bitmap(img, width, height))
                {
                    previewData.Preview = (Image)bmp.GetScaled(previewHeight * 2, previewHeight);
                    previewData.Annotation = $"{width} x {height}";
                }
            }

            return previewData;
        }

        private int HiMetricToPx(double hiMetric)
        {
            const double HIMETRIC_PER_INCH = 2540d;
            
            // we will use the default dpi, because the final image will be rescaled and the dpi setting here doesn't matter much
            return (int)Math.Round(96d * hiMetric / HIMETRIC_PER_INCH);
        }
    }
}
