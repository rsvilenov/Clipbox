using Clipbox.Core.Extensibility.Contracts;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.ViewModels;
using Clipbox.Views.FilterBand.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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




        private string WindowCaptionSearchString
        {
            get { return (string)GetValue(WindowCaptionSearchStringProperty); }
            set { SetValue(WindowCaptionSearchStringProperty, value); }
        }

        private static readonly DependencyProperty WindowCaptionSearchStringProperty =
            DependencyProperty.Register(nameof(WindowCaptionSearchString), typeof(string), typeof(ApplicationSelector), new PropertyMetadata(null));



        private BitmapSource _explorerIcon;

        private BitmapSource GetApplicationIcon(IApplicationInfo appInfo, byte[] iconByteArray)
        {
            string explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            if (Path.Equals(appInfo.ApplicationPath, explorerPath))
            {
                if (_explorerIcon == null)
                {
                    using (Icon appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location))
                    using (Bitmap appIconBmp = appIcon.ToBitmap())
                    {
                        _explorerIcon = appIconBmp.ToBitmapSource();
                    }
                }

                return _explorerIcon;
            }
            else
            {
                return Infrastructure.UI.Utils.Imaging.BitmapSourceFromBitmapByteArray(iconByteArray);
            }
        }

        protected override void LoadData()
        {
            _appInfoCollection = ClipboardItemsDataProvider
                .GetAllSourceAppsAndIcons()
                .Where(i => !string.IsNullOrWhiteSpace(i.Key.ApplicationPath) && i.Value != null)
                .GroupBy(i => i.Key)
                .Select(g => g.First())
                .Select(i => new SourceApplicationInfo
                {
                    Icon = GetApplicationIcon(i.Key, i.Value),
                    ExecutablePath = i.Key.ApplicationPath,
                    ExecutableName = System.IO.Path.GetFileNameWithoutExtension(i.Key.ApplicationPath),
                    ProductName = i.Key.ProductName,
                    FileDescription = i.Key.FileDescription,
                    FormattedProductName = AppDescriptionFormatter.Format(i.Key)
                })
                .ToList();
        }

        protected override void OnLoadDataComplete()
        {
            AppInfoCollection = new AsyncObservableCollection<SourceApplicationInfo>(_appInfoCollection);
        }

        protected override void OnOkButton()
        {
            AppInfoSelected?.Invoke(this, new ApplicationInfoEventArgs(WindowCaptionSearchString, SelectedApps));
        }

        protected override bool ShouldOkButtonBeEnabled()
        {
            return SelectedApps.Any() || !string.IsNullOrWhiteSpace(WindowCaptionSearchString);
        }

        protected override void OnCancelButton()
        {
            AppInfoSelected?.Invoke(this, new ApplicationInfoEventArgs(null, Enumerable.Empty<SourceApplicationInfo>()));
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            AppInfoCollection.Clear();
            IsLoading = true;
            string searchText = searchTextBox.Text;
            List<SourceApplicationInfo> selectedApps = SelectedApps.ToList();

            Task.Run(() =>
            {
                IList<SourceApplicationInfo> filteredAppList = _appInfoCollection
                .Where(sai => sai.ExecutableName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1
                    || sai.FormattedProductName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                .Where(sai => !selectedApps.Contains(sai))
                .ToList();

                Dispatcher
                .BeginInvoke(DispatcherPriority.Background, new Action(() => AppInfoCollection.AddRange(filteredAppList)))
                .Task.ContinueWith((t) => Dispatcher.Invoke(() => IsLoading = false));
            });
            
        }
    }
}
