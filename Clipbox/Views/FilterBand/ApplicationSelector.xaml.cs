using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Views.FilterBand.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipbox.Views.FilterBand
{
    /// <summary>
    /// Interaction logic for ApplicationSelector.xaml
    /// </summary>
    public partial class ApplicationSelector : FilterDialogBase
    {
        public ApplicationSelector()
        {
            InitializeComponent();
        }

        public ApplicationSelector(IClipboardItemsDataProvider clipboardItemsDataProvider) 
            : base(clipboardItemsDataProvider)
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;
        }

        private List<SourceApplicationInfo> _appInfoCollection;
        
        public event EventHandler<ApplicationInfoEventArgs> AppInfoSelected;
        
        public AsyncObservableCollection<SourceApplicationInfo> AppInfoCollection
        {
            get { return (AsyncObservableCollection<SourceApplicationInfo>)GetValue(AppInfoCollectionProperty); }
            set { SetValue(AppInfoCollectionProperty, value); }
        }

        public static readonly DependencyProperty AppInfoCollectionProperty =
            DependencyProperty.Register(nameof(AppInfoCollection), typeof(AsyncObservableCollection<SourceApplicationInfo>), typeof(ApplicationSelector), new PropertyMetadata(null));
        
        protected override void LoadData()
        {
            _appInfoCollection = ClipboardItemsDataProvider
                .GetAllSourceAppsAndIcons()
                .Where(i => !string.IsNullOrWhiteSpace(i.Key) && i.Value != null)
                .GroupBy(i => i.Key)
                .Select(g => g.First())
                .Select(i => new SourceApplicationInfo
                {
                    Icon = Infrastructure.UI.Utils.Imaging.BitmapSourceFromBitmapByteArray(i.Value),
                    ExecutableName = System.IO.Path.GetFileNameWithoutExtension(i.Key)
                })
                .ToList();
        }

        protected override void OnLoadDataComplete()
        {
            AppInfoCollection = new AsyncObservableCollection<SourceApplicationInfo>(_appInfoCollection);
        }

        protected override void OnOkButton()
        {
            AppInfoSelected?.Invoke(this, new ApplicationInfoEventArgs("test", "test"));
        }

        protected override void OnCancelButton()
        {
            AppInfoSelected?.Invoke(this, new ApplicationInfoEventArgs(null, null));
        }

        public class SourceApplicationInfo : BindableObject
        {
            private string _executableName;
            public string ExecutableName
            {
                get { return _executableName; }
                set { SetField(ref _executableName, value); }
            }

            private BitmapSource _icon;
            public BitmapSource Icon
            {
                get { return _icon; }
                set { SetField(ref _icon, value); }
            }
        }

    }
}
