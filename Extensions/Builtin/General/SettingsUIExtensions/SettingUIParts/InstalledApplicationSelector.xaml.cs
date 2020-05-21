using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.BindableObjects;
using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.EventArgsClasses;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for ApplicationSelector.xaml
    /// </summary>
    public partial class InstalledApplicationSelector : UserControl
    {
        public ICommand AddApplication { get; }
        public ICommand RemoveApplication { get; }
        public ICommand BrowseApplication { get; }
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public event EventHandler<InstalledApplicationInfoEventArgs> AppInfoSelected;

        public InstalledApplicationSelector()
        {
            InitializeComponent();

            AddApplication = new DelegateCommand<InstalledApplicationInfo>(AddApplicationHandler);
            RemoveApplication = new DelegateCommand<InstalledApplicationInfo>(RemoveApplicationHandler);
            BrowseApplication = new DelegateCommand(BrowseApplicationHandler);
            OkClicked = new DelegateCommand(OnOkButton, ShouldOkButtonBeEnabled);
            CancelClicked = new DelegateCommand(OnCancelButton);

            SelectedApps = new AsyncObservableCollection<InstalledApplicationInfo>();
            this.mainBorder.DataContext = this;
            Loaded += InstalledApplicationSelector_Loaded;

        }

        private void BrowseApplicationHandler()
        {
            HwndSource source = (HwndSource)HwndSource.FromVisual(this);

            bool isTopMostSet = WindowUtils.CheckWindowStyleIsSet(source.Handle, Win32.Native.NativeEnums.WindowStylesEx.WS_EX_TOPMOST);

            if (isTopMostSet)
            {
                // we need to show the file dialog on top of our window/popup
                // and if we do not make ourselves "no topmost", it will show
                // up behind us and stay there whatever we do with the mouse/kb
                // to bring it on top.
                SetNoTopMost(source.Handle);
            }

            try
            {

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                dlg.DefaultExt = ".exe";
                dlg.Filter = "Application Files (.exe)|*.exe";

                bool? result = dlg.ShowDialog();


                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string fileName = dlg.FileName;

                    bool fileNameExists = false;
                    try
                    {
                        fileNameExists = File.Exists(fileName);
                    }
                    catch { }

                    if (fileNameExists)
                    {
                        var pathAppInfo = ApplicationUtils.GetAppInfoFromPath(fileName);
                        string productName = pathAppInfo.DisplayName;

                        if (string.IsNullOrWhiteSpace(productName))
                        {
                            productName = Path.GetFileNameWithoutExtension(fileName);
                        }

                        var installedAppInfo = new InstalledApplicationInfo
                        {
                            InstallationLocation = pathAppInfo.InstallLocation,
                            ProductName = productName
                        };
                        
                        if (pathAppInfo.DisplayIcon != null)
                        {
                            using (pathAppInfo.DisplayIcon)
                            using (Bitmap bmp = pathAppInfo.DisplayIcon.ToBitmap())
                            {
                                installedAppInfo.Icon = bmp.ToBitmapSource();
                            }
                        }

                        if (!SelectedApps.Any(ap => ap.ProductName == installedAppInfo.ProductName))
                        {
                            SelectedApps.Add(installedAppInfo);
                        }
                    }
                }
            }
            finally
            {
                if (isTopMostSet)
                {
                    // restore the topmost style
                    SetTopMost(source.Handle);
                }
            }
        }

        public static void SetNoTopMost(IntPtr hWnd)
        {
            User32.GetWindowRect(hWnd, out NativeStructures.RECT lpRect);
            int ret = User32.SetWindowPos(hWnd, -1, lpRect.Left, lpRect.Top, lpRect.Width, lpRect.Height, 0);
            if (ret == 0)
                MarshalExt.ThrowOnWin32Error();
        }

        public static void SetTopMost(IntPtr hWnd)
        {
            User32.GetWindowRect(hWnd, out NativeStructures.RECT lpRect);
            int ret = User32.SetWindowPos(hWnd, 0, lpRect.Left, lpRect.Top, lpRect.Width, lpRect.Height, (int)NativeEnums.SetWindowPosFlags.SWP_FRAMECHANGED);
            if (ret == 0)
                MarshalExt.ThrowOnWin32Error();
        }

        private void InstalledApplicationSelector_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            IsLoading = true;

            Task.Run(() =>
            {
                LoadData();

                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        OnLoadDataComplete();
                        IsLoading = false;
                    }));
            });
        }

        
        private void RemoveApplicationHandler(InstalledApplicationInfo obj)
        {
            SelectedApps.Remove(obj);
            if (_appInfoCollection.Contains(obj))
            {
                AppInfoCollection.Add(obj);
            }
        }

        private void AddApplicationHandler(InstalledApplicationInfo obj)
        {
            SelectedApps.Add(obj);
            AppInfoCollection.Remove(obj);
        }

        private List<InstalledApplicationInfo> _appInfoCollection;
        
        public AsyncObservableCollection<InstalledApplicationInfo> AppInfoCollection
        {
            get { return (AsyncObservableCollection<InstalledApplicationInfo>)GetValue(AppInfoCollectionProperty); }
            set { SetValue(AppInfoCollectionProperty, value); }
        }

        public static readonly DependencyProperty AppInfoCollectionProperty =
            DependencyProperty.Register(nameof(AppInfoCollection), typeof(AsyncObservableCollection<InstalledApplicationInfo>), typeof(InstalledApplicationSelector), new PropertyMetadata(null));

        public AsyncObservableCollection<InstalledApplicationInfo> SelectedApps
        {
            get { return (AsyncObservableCollection<InstalledApplicationInfo>)GetValue(SelectedAppsProperty); }
            set { SetValue(SelectedAppsProperty, value); }
        }

        public static readonly DependencyProperty SelectedAppsProperty =
            DependencyProperty.Register(nameof(SelectedApps), typeof(AsyncObservableCollection<InstalledApplicationInfo>), typeof(InstalledApplicationSelector), new PropertyMetadata(null));




        private string WindowCaptionSearchString
        {
            get { return (string)GetValue(WindowCaptionSearchStringProperty); }
            set { SetValue(WindowCaptionSearchStringProperty, value); }
        }

        private static readonly DependencyProperty WindowCaptionSearchStringProperty =
            DependencyProperty.Register(nameof(WindowCaptionSearchString), typeof(string), typeof(InstalledApplicationSelector), new PropertyMetadata(null));



        /// <summary>
        /// Indicates that the code in LoadData() is executing
        /// </summary>
        protected bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        protected static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(InstalledApplicationSelector), new PropertyMetadata(false));

        
        private void LoadData()
        {
            _appInfoCollection = new List<InstalledApplicationInfo>();
            
            foreach (var ia in ApplicationUtils.GetAllInstalledApplications()
                .OrderBy(ap => ap.DisplayName))
            {
                var appInfo = new InstalledApplicationInfo
                {
                    InstallationLocation = ia.InstallLocation,
                    ProductName = ia.DisplayName
                };

                if (ia.DisplayIcon != null)
                {
                    using (ia.DisplayIcon)
                    using (Bitmap bmp = ia.DisplayIcon.ToBitmap())
                    {
                        appInfo.Icon = bmp.ToBitmapSource();
                    }
                }

                _appInfoCollection.Add(appInfo);
            }
        }

        private void OnLoadDataComplete()
        {
            AppInfoCollection = new AsyncObservableCollection<InstalledApplicationInfo>(_appInfoCollection);
        }

        private void OnOkButton()
        {
            AppInfoSelected?.Invoke(this, new InstalledApplicationInfoEventArgs(WindowCaptionSearchString, SelectedApps));
        }

        private bool ShouldOkButtonBeEnabled()
        {
            return SelectedApps.Any() || !string.IsNullOrWhiteSpace(WindowCaptionSearchString);
        }

        private void OnCancelButton()
        {
            AppInfoSelected?.Invoke(this, new InstalledApplicationInfoEventArgs(null, Enumerable.Empty<InstalledApplicationInfo>()));
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            AppInfoCollection.Clear();
            IsLoading = true;
            string searchText = searchTextBox.Text;
            List<InstalledApplicationInfo> selectedApps = SelectedApps.ToList();

            Task.Run(() =>
            {
                IList<InstalledApplicationInfo> filteredAppList = _appInfoCollection
                .Where(sai => sai.ProductName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                .Where(sai => !selectedApps.Contains(sai))
                .ToList();

                Dispatcher
                .BeginInvoke(DispatcherPriority.Background, new Action(() => AppInfoCollection.AddRange(filteredAppList)))
                .Task.ContinueWith((t) => Dispatcher.Invoke(() => IsLoading = false));
            });
            
        }
        
    }
}
