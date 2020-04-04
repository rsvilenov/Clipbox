using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels;
using Clipbox.Views.FilterBand.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Clipbox.Views.FilterBand
{
    /// <summary>
    /// Interaction logic for ApplicationSelector.xaml
    /// </summary>
    public partial class ApplicationSelector : FilterDialogBase
    {
        public ICommand AddApplication { get; set; }
        public ICommand RemoveApplication { get; set; }

        public ApplicationSelector()
        {
            InitializeComponent();
        }

        public ApplicationSelector(IClipboardItemsDataProvider clipboardItemsDataProvider) 
            : base(clipboardItemsDataProvider)
        {
            InitializeComponent();

            AddApplication = new DelegateCommand<SourceApplicationInfo>(AddApplicationHandler);
            RemoveApplication = new DelegateCommand<SourceApplicationInfo>(RemoveApplicationHandler);
            SelectedApps = new AsyncObservableCollection<SourceApplicationInfo>();
            this.mainBorder.DataContext = this;
        }

        private void RemoveApplicationHandler(SourceApplicationInfo obj)
        {
            SelectedApps.Remove(obj);
            AppInfoCollection.Add(obj);
        }

        private void AddApplicationHandler(SourceApplicationInfo obj)
        {
            SelectedApps.Add(obj);
            AppInfoCollection.Remove(obj);
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

        public AsyncObservableCollection<SourceApplicationInfo> SelectedApps
        {
            get { return (AsyncObservableCollection<SourceApplicationInfo>)GetValue(SelectedAppsProperty); }
            set { SetValue(SelectedAppsProperty, value); }
        }

        public static readonly DependencyProperty SelectedAppsProperty =
            DependencyProperty.Register(nameof(SelectedApps), typeof(AsyncObservableCollection<SourceApplicationInfo>), typeof(ApplicationSelector), new PropertyMetadata(null));


        protected override void LoadData()
        {
            _appInfoCollection = ClipboardItemsDataProvider
                .GetAllSourceAppsAndIcons()
                .Where(i => !string.IsNullOrWhiteSpace(i.Key.ApplicationPath) && i.Value != null)
                .GroupBy(i => i.Key)
                .Select(g => g.First())
                .Select(i => new SourceApplicationInfo
                {
                    Icon = Infrastructure.UI.Utils.Imaging.BitmapSourceFromBitmapByteArray(i.Value),
                    ExecutableName = System.IO.Path.GetFileNameWithoutExtension(i.Key.ApplicationPath),
                    ProductName = AppDescriptionFormatter.Format(i.Key)
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

            private string _productName;
            public string ProductName
            {
                get { return _productName; }
                set { SetField(ref _productName, value); }
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
