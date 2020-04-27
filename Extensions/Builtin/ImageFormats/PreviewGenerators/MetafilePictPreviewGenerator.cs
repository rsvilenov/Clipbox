using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Win32.Native;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.PreviewGenerators
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{D77A8959-A11B-457E-892F-1D34BC925E87}")]
    public class MetafilePictPreviewGenerator : SinglePreviewGenerator<MetafilePictIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(MetafilePictIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            // TODO: support different mapping modes
            if (intermediateFormat.MappingMode != NativeEnums.DeviceContextMapMode.HiMetric)
            {
                Debug.WriteLine($"Mapping mode '{intermediateFormat.MappingMode}' will be ignored. 'HiMetric' will be used instead, so the final image's dimentions may be wrong.");
            }

            // Convert the old metafile format to enhanced metafile
            IntPtr enhMetafilePtr = Gdi32.SetWinMetaFileBits((uint)intermediateFormat.MetafileBytes.Length, intermediateFormat.MetafileBytes, IntPtr.Zero, IntPtr.Zero);

            var previewData = new ImagePreviewData();

            // convert the HiMetric to pixels
            int width = HiMetricToPx(intermediateFormat.UnitsX);
            int height = HiMetricToPx(intermediateFormat.UnitsY);

            // Create a Metafile instance and delete the handle
            using (Image img = new Metafile(enhMetafilePtr, true /* 'true' deletes the hMetafile */))
            {
                // I haven't found a way to set the metafile's dimentions upon its creation, so I will
                // copy the newly created image (which has fullHD dimentions when I test the code) to
                // an empty bitmap with the rigth width and height
                using (Image bmp = new Bitmap(img, width, height))
                {
                    previewData.Preview = (Image)bmp.GetScaled(previewParams.PreviewHeight * 2, previewParams.PreviewHeight);
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
