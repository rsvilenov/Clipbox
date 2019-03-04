using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemFactory
    {
        private IClipboardFormatPreviewExtentionContainer _container;

        private int _maxPreviewItemsInGroupItem;

        public ClipboardViewItemFactory(IClipboardFormatPreviewExtentionContainer container)
            : this(container, 0)
        {
        }

        public ClipboardViewItemFactory(IClipboardFormatPreviewExtentionContainer container, int maxPreviewItemsInGroupItem)
        {
            Debug.Assert(container != null);

            _container = container;
            
            _maxPreviewItemsInGroupItem = maxPreviewItemsInGroupItem;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public ClipboardViewItem Create(IClipboardItem clipboardItem, int previewWidth, int previewHeight)
        {
            if (clipboardItem == null)
            {
                throw new ArgumentNullException("clipboardIitem");
            }

            if (clipboardItem.Source == null)
            {
                throw new ArgumentNullException("clipboardIitem.Source");
            }

            if (clipboardItem.Source.SourceWindowInfo == null)
            {
                throw new ArgumentNullException("clipboardIitem.Source.SourceWindowInfo");
            }

            IPreviewHandler previewHandler = null;
            ClipboardDataObject cdoForPreview = null;
            // get the first preview handler - it should be for the clipboard format with the highest priority
            foreach (ClipboardDataObject cdo in clipboardItem.ClipboardDataObjects)
            {
                previewHandler = _container.ResolveClipboardPreviewHandler(cdo.Format);
                if (previewHandler != null)
                {
                    cdoForPreview = cdo;
                    break;
                }
            }

            // TODO: create a default preview handler
            if (previewHandler == null)
            {
                throw new FormatException($"Unable to find a suitable preview handler for this item.");
            }

            ClipboardViewItem viewItem = null;
            if (previewHandler is IManyPreviewHandler)
            {
                var manyPreviewHandler = previewHandler as IManyPreviewHandler;


                try { manyPreviewHandler.SetData(cdoForPreview.OriginalData); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewHandler.SetData), manyPreviewHandler.GetType(), ex); }

                int itemsCount = 0;
                try { itemsCount = manyPreviewHandler.GetItemsTotalCount(); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewHandler.SetData), manyPreviewHandler.GetType(), ex); }

                if (itemsCount == 1)
                {
                    var singleViewItem = new ClipboardViewItemSingle();
                    singleViewItem.PreviewItem = GetClipboardPreviewItem(cdoForPreview.OriginalData, previewHandler, previewWidth, previewHeight);
                    viewItem = singleViewItem;
                }
                else if (itemsCount > 1)
                {
                    var groupItem = new ClipboardViewItemGroup
                    {
                        TotalClipboardItemsCount = itemsCount
                    };
                    var previewItems = GetClipboardViewItemsForGroup(manyPreviewHandler, itemsCount, previewWidth, previewHeight);
                    groupItem.PreviewItems = new ObservableCollection<ClipboardViewSubItemBase>(previewItems);
                    viewItem = groupItem;
                }
                else
                {
                    throw new ClipboxExtentionException($"The preview extention returned invalid items count: {itemsCount}.", manyPreviewHandler.GetType());
                }
            }
            else
            {
                var singleViewItem = new ClipboardViewItemSingle();
                singleViewItem.PreviewItem = GetClipboardPreviewItem(cdoForPreview.OriginalData, previewHandler, previewWidth, previewHeight);
                viewItem = singleViewItem;
            }

            viewItem.SourceWindowIcon = BitmapSourceFromBitmapByteArray(clipboardItem.Source.SourceWindowInfo.Icon);
            viewItem.SourceWindowCaption = clipboardItem.Source.SourceWindowInfo.Caption;
            viewItem.DateCreated = new DateTime(clipboardItem.EntryDateTicks);
            viewItem.DateModified = new DateTime(clipboardItem.ModifiedDateTicks);
            viewItem.ClipboardItemId = clipboardItem.Id;
            viewItem.IsPinned = clipboardItem.IsPinned;
            IEnumerable<ClipboardContentClass> contentClasses = clipboardItem.ClipboardDataObjects.Select(cdo => cdo.ContentClass).Distinct();
            viewItem.ContentClasses = new AsyncObservableCollection<ClipboardContentClass>(contentClasses);

            return viewItem;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private IEnumerable<ClipboardViewSubItemBase> GetClipboardViewItemsForGroup(IManyPreviewHandler previewHandler, int itemsCount, int previewWidth, int previewHeight)
        {
            Debug.Assert(previewHandler != null);

            List<ClipboardViewSubItemBase> viewItems = new List<ClipboardViewSubItemBase>();
            
            if (previewHandler.PreviewDataType == typeof(FilePreviewData))
            {
                IEnumerable<FilePreviewData> previewDataCollection;
                var filePreviewHandler = previewHandler as ManyPreviewHandler<FilePreviewData>;
                //if (itemsCount > 4)
                //{
                //    try { previewDataCollection = filePreviewHandler.GetPreview(GlobalConstants.PREVIEW_MANY_ITEMS_ITEM_WIDTH, GlobalConstants.PREVIEW_MANY_ITEMS_ITEM_HEIGHT, _maxPreviewItemsInGroupItem); }
                //    catch (Exception ex) { throw new ClipboxExtentionException(nameof(filePreviewHandler.GetPreview), filePreviewHandler.GetType(), ex); }
                //}
                //else
                //{
                    try { previewDataCollection = filePreviewHandler.GetPreview(previewWidth, previewHeight, _maxPreviewItemsInGroupItem); }
                    catch (Exception ex) { throw new ClipboxExtentionException(nameof(filePreviewHandler.GetPreview), filePreviewHandler.GetType(), ex); }
                //}

                foreach (var previewData in previewDataCollection)
                {
                    var previewItem = ClipboardPreviewItem.FromFilePreviewData(previewData);
                    ClipboardViewSubItem viewItem = new ClipboardViewSubItem
                    {
                        PreviewItem = previewItem
                    };
                    viewItems.Add(viewItem);
                }

                int itemsWithNoPreview = itemsCount - previewDataCollection.Count();
                if (itemsWithNoPreview > 0)
                {
                    ClipboardViewSubItemNMore viewItem = new ClipboardViewSubItemNMore
                    {
                        NumberOfItems = itemsWithNoPreview
                    };
                    viewItems.Add(viewItem);
                }
            }
            else
            {
                throw new NotImplementedException($"{previewHandler.PreviewDataType.Name} is not supported yet.");
            }

            return viewItems;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private ClipboardPreviewItem GetClipboardPreviewItem(object data, IPreviewHandler previewHandler, int previewWidth, int previewHeight)
        {
            Debug.Assert(previewHandler != null);

            if (previewHandler.PreviewDataType == typeof(ImagePreviewData))
            {
                PreviewHandler<ImagePreviewData> imagePreviewHandler = (PreviewHandler<ImagePreviewData>)previewHandler;
                ImagePreviewData imagePreviewData = null;

                try { imagePreviewData = imagePreviewHandler.GetPreview(data, previewWidth, previewHeight); } // TODO: find a way to determine the preview dimentions
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(imagePreviewHandler.GetPreview), previewHandler.GetType(), ex); }

                return ClipboardPreviewItem.FromImagePreviewData(imagePreviewData);
            }
            else if (previewHandler.PreviewDataType == typeof(FilePreviewData))
            {
                ManyPreviewHandler<FilePreviewData> filePreviewHandler = (ManyPreviewHandler<FilePreviewData>)previewHandler;
                IEnumerable<FilePreviewData> previewDataCollection = Enumerable.Empty<FilePreviewData>(); 

                try { previewDataCollection = filePreviewHandler.GetPreview(previewWidth, previewHeight, _maxPreviewItemsInGroupItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(filePreviewHandler.GetPreview), previewHandler.GetType(), ex); }

                Debug.Assert(previewDataCollection != null);
                Debug.Assert(previewDataCollection.Any());

                return ClipboardPreviewItem.FromFilePreviewData(previewDataCollection.First());
            }
            else
            {
                throw new NotImplementedException($"{previewHandler.PreviewDataType.Name} is not supported yet.");
            }
        }

        private BitmapSource BitmapSourceFromBitmapByteArray(byte[] bitmapByteArray)
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
