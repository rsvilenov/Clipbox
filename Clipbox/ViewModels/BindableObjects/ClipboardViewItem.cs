using Clipbox.Core.Enums;
using Clipbox.Infrastructure.UI;
using System;
using System.Collections.ObjectModel;
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

        private bool _markAsUnselectable;

        public bool MarkAsUnselectable
        {
            get { return _markAsUnselectable; }
            private set { SetField(ref _markAsUnselectable, value); }
        }

        public void SignalThatItemIsUnselectable()
        {
            MarkAsUnselectable = true;
            MarkAsUnselectable = false;
        }
    }
}
