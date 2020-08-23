using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.PreviewGenerators
{
    [ClipboxExtension(
        description: "Preview generation support for copied files/folders",
        author: ProductConstants.PRODUCT_NAME,
        version: 1.0,
        MinHostVersion = 1.0
        )]
    [IntermediateFormatSupport("{3D2C252F-7838-49C2-9D5D-C6B4C92320D3}")]
    public class FilesystemPathsPreviewGenerator : ManyPreviewGenerator<FilesystemPathsIntermediateFormat, FilePreviewData>
    {
        private bool IsSmallIconFiletype(string path)
        {
            if (path.EndsWithAny(".exe", ".cur", ".rar", ".zip", ".7z"))
            {
                return true;
            }

            return false;
        }

        private Bitmap GetFileThumbnail(int previewWidth, int previewHeight, string path)
        {
            Bitmap thumbnail = null;

            try
            {
                if (IsSmallIconFiletype(path))
                {
                    thumbnail = WindowsThumbnailProvider.GetThumbnail(path, 64, 64, ThumbnailOptions.IconOnly);
                }
                else
                {
                    if (previewWidth > 64)
                    {
                        thumbnail = WindowsThumbnailProvider.GetThumbnail(path, 256, 256, ThumbnailOptions.BiggerSizeOk); // | ThumbnailOptions.ScaleUp | ThumbnailOptions.CropToSquare);
                    }
                    else
                    {
                        thumbnail = WindowsThumbnailProvider.GetThumbnail(path, previewWidth, previewHeight, ThumbnailOptions.IconOnly);
                    }
                }
            }
            catch (FileNotFoundException) { }
            catch (IOException) { }

            return thumbnail;
        }

        protected override IEnumerable<FilePreviewData> GetPreview(FilesystemPathsIntermediateFormat intermediateFormatObj, PreviewParams previewParams, int maxNumberOfPreviewsToGenerate)
        {
            List<FilePreviewData> previewList = new List<FilePreviewData>();

            int previewCount = 0;

            foreach (string path in intermediateFormatObj.Paths)
            {
                if (maxNumberOfPreviewsToGenerate > 0 && maxNumberOfPreviewsToGenerate <= previewCount++)
                {
                    break;
                }

                Image preview = null;

                Bitmap thumbnail = GetFileThumbnail(previewParams.PreviewWidth, previewParams.PreviewHeight, path);

                // if one of the previews fails to be generated, fail the whole task.
                if (thumbnail == null)
                {
                    return null;
                }

                if (thumbnail.Width == previewParams.PreviewWidth && thumbnail.Height == previewParams.PreviewHeight)
                {
                    preview = thumbnail;
                }
                else if (thumbnail.Width > previewParams.PreviewWidth && thumbnail.Height > previewParams.PreviewHeight)
                {
                    using (thumbnail)
                        preview = thumbnail.GetScaled(previewParams.PreviewWidth, previewParams.PreviewHeight);
                }
                else
                {
                    using (thumbnail)
                        preview = thumbnail.GetCenteredOverTransparentCanvas(previewParams.PreviewWidth, previewParams.PreviewHeight);
                }


                previewList.Add(new FilePreviewData
                {
                    Path = path,
                    Annotation = Path.GetFileName(path),
                    Preview = preview
                });

            }


            return previewList;
        }

        protected override int GetItemsTotalCount(FilesystemPathsIntermediateFormat intermediateFormatObj)
        {
            return intermediateFormatObj.Paths.Count();
        }
    }
}
