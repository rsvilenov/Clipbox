using Clipbox.Core.Extensibility.Attributes;
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
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
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

            return thumbnail;
        }

        protected override IEnumerable<FilePreviewData> GetPreview(FilesystemPathsIntermediateFormat intermediateFormatObj, int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate)
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

                Bitmap thumbnail = GetFileThumbnail(previewWidth, previewHeight, path);

                if (thumbnail != null)
                {
                    if (thumbnail.Width == previewWidth && thumbnail.Height == previewHeight)
                    {
                        preview = thumbnail;
                    }
                    else if (thumbnail.Width > previewWidth && thumbnail.Height > previewHeight)
                    {
                        using (thumbnail)
                            preview = thumbnail.GetScaled(previewWidth, previewHeight);
                    }
                    else
                    {
                        using (thumbnail)
                            preview = thumbnail.GetCenteredOverTransparentCanvas(previewWidth, previewHeight);
                    }
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
