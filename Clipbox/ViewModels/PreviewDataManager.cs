using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
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
                cvi = _clipboardViewItemFactory.CreateFromStoredPreviewData(storedData, clipboardItem);
            }
            else
            {
                cvi = _clipboardViewItemFactory.CreateFromClipboardItem(clipboardItem, previewHeight, previewHeight);
                StorePreviewDataToCache(cvi, clipboardItem.Id, previewWidth, previewHeight);
            }

            return cvi;
        }

        public void DeleteUnusedStoredPreviewData(IEnumerable<IClipboardItem> existingClipboardItems)
        {
            var relativeFilePathPrefixesToKeep = existingClipboardItems.Select(ci => $"{ci.Id}_");
            foreach (string relativeFileName in AppDataUtils.ListFiles(APP_DATA_SUBDIR))
            {
                if (!relativeFilePathPrefixesToKeep.Any(p => relativeFileName.StartsWith(p)))
                {
                    AppDataUtils.DeleteFile(Path.Combine(APP_DATA_SUBDIR, relativeFileName));
                }
            }
        }

        private StoredPreviewData GetStoredPreviewData(string clipboardItemId, int previewWidth, int previewHeight)
        {

            StoredPreviewData data = null;

            string relativeFilePath = Path.Combine(APP_DATA_SUBDIR, $"{clipboardItemId}_{previewWidth}_{previewHeight}");
            if (AppDataUtils.CheckFileExists(relativeFilePath))
            {
                byte[] fileData = AppDataUtils.ReadBytesFromApplicationData(relativeFilePath);

                // deserialize
                using (var ms = new MemoryStream(fileData))
                {
                    var formatter = new BinaryFormatter();
                    data = (StoredPreviewData)formatter.Deserialize(ms);
                }
            }

            return data;
        }
        
        private void StorePreviewDataToCache(ClipboardViewItem cvi, string clipboardItemId, int previewWidth, int previewHeight)
        {
            StoredPreviewData spd = CreateStoredPreviewDataObject(cvi);

            // serialize
            byte[] serializedData = null;

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, spd);
                serializedData = ms.ToArray();
            }

            EnsureAppDataSubDirExists();

            string fileName = $"{clipboardItemId}_{previewWidth}_{previewHeight}";

            AppDataUtils.WriteToApplicationData(Path.Combine(APP_DATA_SUBDIR, fileName), serializedData);
        }

        private static void EnsureAppDataSubDirExists()
        {
            string dirPath = Path.Combine(AppDataUtils.GetAppDataDir(throwIfNotExists: false), APP_DATA_SUBDIR);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        private static StoredPreviewData CreateStoredPreviewDataObject(ClipboardViewItem cvi)
        {
            StoredPreviewData data = new StoredPreviewData();

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

                int? nMore = previewItems
                    .Where(pi => pi is ClipboardViewSubItemNMore)
                    .Cast<ClipboardViewSubItemNMore>()
                    .SingleOrDefault()?.NumberOfItems;

                data.NMore = nMore.GetValueOrDefault();
            }

            return data;
        }
    }
}
