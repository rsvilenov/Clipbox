using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.Services.DataObjects;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels
{
    public static class ClipboardViewItemFactory
    {
        public static ClipboardViewItem CreateFromPreviewDataTransferObject(IClipboardItem clipboardItem, PreviewDataTransferObject pdto)
        {
            ClipboardViewItem viewItem = null;

            if (pdto.IsGroup)
            {
                var previewItems = pdto.PreviewItems
                    .Select(spi =>
                    new ClipboardViewSubItem
                    {
                        PreviewItem = GetClipboardPreviewItem(spi)
                    })
                    .Cast<ClipboardViewSubItemBase>()
                    .ToList();

                if (pdto.NumberOfItemsWithoutPreview > 0)
                {
                    previewItems.Add(new ClipboardViewSubItemNMore
                    {
                        NumberOfItems = pdto.NumberOfItemsWithoutPreview
                    });
                }

                viewItem = new ClipboardViewItemGroup
                {
                    PreviewItems = new ObservableCollection<ClipboardViewSubItemBase>(previewItems),
                    // TODO: Remove this property and make it consistent with the way MultiItemLoading.xaml shows the total count
                    TotalClipboardItemsCount = pdto.NumberOfItemsWithoutPreview + pdto.PreviewItems.Count()
                };
            }
            else
            {
                PreviewItemTransferObject pito = pdto.PreviewItems.Single();
                viewItem = new ClipboardViewItemSingle
                {
                    PreviewItem = GetClipboardPreviewItem(pito)
                };
            }

            if (clipboardItem.Source.SourceWindowInfo.Icon != null)
            {
                viewItem.SourceWindowIcon = BitmapSourceFromBitmapByteArray(clipboardItem.Source.SourceWindowInfo.Icon);
            }
            else
            {
                // TODO: show a defalt icon here
            }

            viewItem.IsCloudItem = clipboardItem.Source.MachineUniqueId != MachineUtils.GetMachineGuid();
            viewItem.SourceWindowMachineName = clipboardItem.Source.MachineName;
            viewItem.SourceWindowUserName = clipboardItem.Source.UserName;
            viewItem.SourceWindowCaption = clipboardItem.Source.SourceWindowInfo.Caption;
            viewItem.DateCreated = new DateTime(clipboardItem.EntryDateTicks);
            viewItem.DateModified = new DateTime(clipboardItem.ModifiedDateTicks);
            viewItem.ClipboardItemId = clipboardItem.Id;
            IEnumerable<ClipboardContentClass> contentClasses = clipboardItem.ClipboardDataObjects.Select(cdo => cdo.ContentClass).Distinct();
            viewItem.ContentClasses = new AsyncObservableCollection<ClipboardContentClass>(contentClasses);
            viewItem.IsPinned = clipboardItem.IsPinned;

            return viewItem;
        }

        public static ClipboardPreviewItem GetClipboardPreviewItem(PreviewItemTransferObject pito)
        {
            if (pito == null)
            {
                throw new ArgumentNullException(nameof(pito));
            }

            var cpi = new ClipboardPreviewItem
            {
                Text = pito.Text,
                Description = pito.Description,
                Image = pito.BitmapData != null ? BitmapSourceFromBitmapByteArray(pito.BitmapData) : null
            };

            return cpi;
        }

        private static BitmapSource BitmapSourceFromBitmapByteArray(byte[] bitmapByteArray)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(bitmapByteArray))
            using (bmp = new Bitmap(ms))
            {
                BitmapSource bms = bmp.ToBitmapSource();
                bms.Freeze();
                return bms;
            }
        }

    }
}
