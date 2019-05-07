using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.ViewModels
{
    public class PreviewDataManager
    {
        private const string APP_DATA_SUBDIR = "PreviewDataCache";

        private ClipboardViewItemFactory _clipboardViewItemFactory;

        public PreviewDataManager(ClipboardViewItemFactory cviFactory)
        {
            _clipboardViewItemFactory = cviFactory;
        }

        public ClipboardViewItem GetClipboardViewItem(IClipboardItem clipboardItem, int previewWidth, int previewHeight)
        {
            if (clipboardItem.IsShrinked)
            {
                throw new ArgumentException("The clipboard item should not be shrinked.");
            }
            
            StoredPreviewData storedData = GetStoredPreviewData(clipboardItem.Id, previewHeight, previewHeight);
            ClipboardViewItem cvi = null;
            if (storedData != null)
            {
                cvi = _clipboardViewItemFactory.FromStoredPreviewData(storedData, clipboardItem);
            }
            else
            {
                cvi = _clipboardViewItemFactory.FromClipboardItem(clipboardItem, previewHeight, previewHeight);
                StorePreviewDataToCache(cvi, clipboardItem.Id, previewWidth, previewHeight);
            }

            return cvi;
        }

        private StoredPreviewData GetStoredPreviewData(string clipboardItemId, int previewWidth, int previewHeight)
        {
            string fileName = $"{clipboardItemId}_{previewWidth}_{previewHeight}";
            string fullFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), fileName);

            StoredPreviewData data = null;

            if (File.Exists(fullFileName))
            {
                // deserialize
                using (var ms = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    data = (StoredPreviewData)formatter.Deserialize(ms);
                }
            }

            return data;
        }
        
        private void StorePreviewDataToCache(ClipboardViewItem cvi, string clipboardItemId, int previewWidth, int previewHeight)
        {
            StoredPreviewData spd = GetStoredPreviewData(cvi);

            // serialize
            byte[] serializedData = null;

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, spd);
            }
            
            string fileName = $"{clipboardItemId}_{previewWidth}_{previewHeight}";
            AppDataUtils.WriteToApplicationData(Path.Combine(APP_DATA_SUBDIR, fileName), serializedData);
        }

        private static StoredPreviewData GetStoredPreviewData(ClipboardViewItem cvi)
        {
            StoredPreviewData data = new StoredPreviewData();
            // store and/or return
            if (cvi is ClipboardViewItemSingle)
            {
                var previewItem = ((ClipboardViewItemSingle)cvi).PreviewItem;

                StoredPreviewItem spd = new StoredPreviewItem
                {
                    ImageData = previewItem.Image?.ToByteArray(),
                    Text = previewItem.Text,
                    Description = previewItem.Description
                };

                data.PreviewItems.Add(spd);
            }
            else if (cvi is ClipboardViewItemGroup)
            {
                var previewItems = ((ClipboardViewItemGroup)cvi).PreviewItems;

                var previewSubItems = previewItems
                    .Where(pi => pi is ClipboardViewSubItem)
                    .Cast<ClipboardViewSubItem>()
                    .Select(cvsi => cvsi.PreviewItem);
                
                var spds = previewSubItems.
                    Select(psi => new StoredPreviewItem
                    {
                        ImageData = psi.Image?.ToByteArray(),
                        Description = psi.Description,
                        Text = psi.Text
                    })
                    .ToList();

                data.PreviewItems.AddRange(spds);
                data.IsGroup = true;
            }

            return data;
        }
    }
}
