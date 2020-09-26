using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Infrastructure.UI;
using System;
using System.Diagnostics;
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

        private string _sourceWindowDescription;

        public string SourceWindowDescription
        {
            get { return _sourceWindowDescription; }
            set { SetField(ref _sourceWindowDescription, value); }
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

        private MachineType _sourceMachineType;

        public MachineType SourceMachineType
        {
            get { return _sourceMachineType; }
            set { SetField(ref _sourceMachineType, value); }
        }

        private bool _isPinned;

        public bool IsPinned
        {
            get { return _isPinned; }
            set { SetField(ref _isPinned, value); }
        }

        private bool _isProtected;

        public bool IsProtected
        {
            get { return _isProtected; }
            set { SetField(ref _isProtected, value); }
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

        private string _clipboardItemId;

        public string ClipboardItemId
        {
            get { return _clipboardItemId; }
            set { SetField(ref _clipboardItemId, value); }
        }

        private string _generatorExtensionTypeName;
        /// <summary>
        /// The type name of the preview generator extension,
        /// used to generate the preview.
        /// </summary>
        public string GeneratorExtensionTypeName
        {
            get { return _generatorExtensionTypeName; }
            set { SetField(ref _generatorExtensionTypeName, value); }
        }

        ~ClipboardViewItem()
        {
            Debug.WriteLine($"GCing viewitem for id {ClipboardItemId}");
        }
    }
}
