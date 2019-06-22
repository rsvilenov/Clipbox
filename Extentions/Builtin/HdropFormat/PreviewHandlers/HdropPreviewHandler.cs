using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Drawing;
using Clipbox.Infrastructure.ExtentionMethods;
using System.IO;

namespace Clipbox.Extentions.BuiltIn.HdropFormat
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_HDROP")]
    public class HdropPreviewHandler : ManyPreviewHandler<FilePreviewData>
    {
        private List<string> _paths;

        public override void SetData(object clipboardData)
        {
            _paths = new List<string>();

            string[] hdropPaths = clipboardData as string[];
            if (hdropPaths == null)
            {
                throw new ArgumentException("clipboardData is not a string[]");
            }

            List<FilePreviewData> previewList = new List<FilePreviewData>();
            foreach (string path in hdropPaths)
            {
                // sometimes I get control characters instead of paths in the beginning of the hdrop string
                if (path.Any(cc => char.IsControl(cc)))
                {
                    continue;
                }

                _paths.Add(path);
            }
        }

        public override int GetItemsTotalCount()
        {
            return _paths.Count;
        }

        private bool IsSmallIconFiletype(string path)
        {
            if (path.EndsWithAny(".exe", ".cur", ".rar", ".zip", ".7z"))
            {
                return true;
            }

            return false;
        }

        public override IEnumerable<FilePreviewData> GetPreview(int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate)
        {
            List<FilePreviewData> previewList = new List<FilePreviewData>();

            int previewCount = 0;

            foreach (string path in _paths)
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
    }
}
