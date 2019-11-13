using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.Enums;
using System;
using System.Linq;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemWrapper : BindableObject
    {
        public ClipboardViewItemWrapper(IClipboardItem clipboardItem)
        {
            // store the original item for searching...
            // TODO: map this item's properties to a viewmodel object
            ClipboardItem = clipboardItem;

            Id = clipboardItem.Id;
            DateCreated = new DateTime(clipboardItem.TimestampInfo.EntryDateTicks);
            IsItemTransferable = clipboardItem.CanTransfer;
        }

        private string _id;

        public string Id
        {
            get { return _id; }
            private set { SetField(ref _id, value); }
        }

        private DateTime _dateCreated;

        public DateTime DateCreated
        {
            get { return _dateCreated; }
            private set
            {
                SetField(ref _dateCreated, value);
                DateCreatedFriendly = GetFriendlyDateString(value);
            }
        }

        private string _dateCreatedFriendly;

        public string DateCreatedFriendly
        {
            get { return _dateCreatedFriendly; }
            private set { SetField(ref _dateCreatedFriendly, value); }
        }

        private ClipboardViewItem _viewItem;

        public ClipboardViewItem ViewItem
        {
            get { return _viewItem; }
            set { SetField(ref _viewItem, value); }
        }

        private bool _isViewItemLoaded;

        public bool IsViewItemLoaded
        {
            get { return _isViewItemLoaded; }
            set { SetField(ref _isViewItemLoaded, value); }
        }

        private bool _isShownIntoView;

        public bool IsShownIntoView
        {
            get { return _isShownIntoView; }
            set { SetField(ref _isShownIntoView, value); }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetField(ref _isSelected, value); }
        }

        private ClipboardViewItemDimensionInfo _dimensionInfo;

        public ClipboardViewItemDimensionInfo DimensionInfo
        {
            get { return _dimensionInfo; }
            set { SetField(ref _dimensionInfo, value); }
        }

        private bool _isItemTransferable;
        /// <summary>
        /// Indicates whether the item can be uploaded to the cloud.
        /// </summary>
        public bool IsItemTransferable
        {
            get { return _isItemTransferable; }
            set { SetField(ref _isItemTransferable, value); }
        }

        private CloudSyncStatus _syncStatus;
        /// <summary>
        /// Indicates whether the item should be uploaded to the cloud
        /// as well as the status of the upload.
        /// </summary>
        public CloudSyncStatus SyncStatus
        {
            get { return _syncStatus; }
            set { SetField(ref _syncStatus, value); }
        }
        
        /// <summary>
        /// We will not bind this item.
        /// </summary>
        public IClipboardItem ClipboardItem { get; private set; }
        
        private string GetFriendlyDateString(DateTime date)
        {
            // TODO: Translate 'Today' and 'Yesterday'.
            return
                date.Date == DateTime.Today ?
                "Today" :
                date.Date == DateTime.Today.AddDays(-1) ?
                "Yesterday" :
                date.Year == DateTime.Now.Year ?
                    date.ToString("dd MMMM") :
                    date.ToString("dd MMMM yyyy");
        }

    }
}
