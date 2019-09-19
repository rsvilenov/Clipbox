using Clipbox.Core.Enums;
using Clipbox.Infrastructure.UI;
using System;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels.BindableObjects
{
    public abstract class ClipboardViewItem : ClipboardViewItemBase
    {
        private BitmapSource _sourceWindowIcon;

        public BitmapSource SourceWindowIcon
        {
            get { return _sourceWindowIcon; }
            set { SetField(ref _sourceWindowIcon, value); }
        }

        private string _sourceWindowCaption;

        public string SourceWindowCaption
        {
            get { return _sourceWindowCaption; }
            set { SetField(ref _sourceWindowCaption, value); }
        }

        private string _sourceWindowMachineName;

        public string SourceWindowMachineName
        {
            get { return _sourceWindowMachineName; }
            set { SetField(ref _sourceWindowMachineName, value); }
        }

        private string _sourceWindowUserName;

        public string SourceWindowUserName
        {
            get { return _sourceWindowUserName; }
            set { SetField(ref _sourceWindowUserName, value); }
        }

        private bool _isCloudItem;
        /// <summary>
        /// Indicates whether the item comes from the cloud and not from local computer.
        /// </summary>
        public bool IsCloudItem
        {
            get { return _isCloudItem; }
            set { SetField(ref _isCloudItem, value); }
        }

        private DateTime _dateCreated;

        public DateTime DateCreated
        {
            get { return _dateCreated; }
            set
            {
                SetField(ref _dateCreated, value);
            }
        }

        private DateTime _dateModified;

        public DateTime DateModified
        {
            get { return _dateModified; }
            set { SetField(ref _dateModified, value); }
        }
        
        private AsyncObservableCollection<ClipboardContentClass> _cotentClasses;

        public AsyncObservableCollection<ClipboardContentClass> ContentClasses
        {
            get { return _cotentClasses; }
            set { SetField(ref _cotentClasses, value); }
        }

        private bool _isPinned;

        public bool IsPinned
        {
            get { return _isPinned; }
            set { SetField(ref _isPinned, value); }
        }

        private string _clipboardItemId;

        public string ClipboardItemId
        {
            get { return _clipboardItemId; }
            set { SetField(ref _clipboardItemId, value); }
        }

        private bool _isItemAnimationRunRequested;

        public bool IsItemAnimationRunRequested
        {
            get { return _isItemAnimationRunRequested; }
            private set { SetField(ref _isItemAnimationRunRequested, value); }
        }

        public void RunItemAnimation()
        {
            IsItemAnimationRunRequested = true;
            IsItemAnimationRunRequested = false;
        }
    }
}
