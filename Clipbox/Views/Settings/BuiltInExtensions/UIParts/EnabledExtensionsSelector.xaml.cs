using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Views.Settings.EventArgsClasses;
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

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for EnabledExtensionsSelector.xaml
    /// </summary>
    public partial class EnabledExtensionsSelector : UserControl
    {
        public ICommand EnableExtension { get; }
        public ICommand DisableExtensions { get; }
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public event EventHandler<EnabledExtensionsEventArgs> EnabledExtensionsChanged;

        private IEnumerable<PersistableExtensionInfo> _enabledExtensions;
        public EnabledExtensionsSelector(IEnumerable<PersistableExtensionInfo> enabledExtensions)
        {
            InitializeComponent();

            _enabledExtensions = enabledExtensions;

            EnabledExtensions = new AsyncObservableCollection<ExtensionDisplayInfo>();
            _allExtensionsCollection = new List<ExtensionDisplayInfo>();

            EnableExtension = new DelegateCommand<ExtensionDisplayInfo>(EnableExtensionHandler);
            DisableExtensions = new DelegateCommand<ExtensionDisplayInfo>(DisableExtensionHandler);
            OkClicked = new DelegateCommand(OkButtonHandler);
            CancelClicked = new DelegateCommand(CancelButtonHandler);

            this.mainBorder.DataContext = this;
            Loaded += InstalledApplicationSelector_Loaded;

        }

        private void SetEnabledExtensions()
        {
            EnabledExtensions.AddRange(_allExtensionsCollection
                .Where(edi => _enabledExtensions.Any(ee => edi.PersistableExtensionInfo.ExtensionObjectTypeFullName == ee.ExtensionObjectTypeFullName 
                && edi.PersistableExtensionInfo.AssemblyFullName == ee.AssemblyFullName
                && edi.PersistableExtensionInfo.AssemblyLocation == ee.AssemblyLocation))
                .ToList());
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


        private void DisableExtensionHandler(ExtensionDisplayInfo obj)
        {
            EnabledExtensions.Remove(obj);
            if (_allExtensionsCollection.Contains(obj))
            {
                AllExtensions.Add(obj);
            }
        }

        private void EnableExtensionHandler(ExtensionDisplayInfo obj)
        {
            EnabledExtensions.Add(obj);
            AllExtensions.Remove(obj);
        }

        private List<ExtensionDisplayInfo> _allExtensionsCollection;

        public AsyncObservableCollection<ExtensionDisplayInfo> AllExtensions
        {
            get { return (AsyncObservableCollection<ExtensionDisplayInfo>)GetValue(AllExtensionsProperty); }
            set { SetValue(AllExtensionsProperty, value); }
        }

        public static readonly DependencyProperty AllExtensionsProperty =
            DependencyProperty.Register(nameof(AllExtensions), typeof(AsyncObservableCollection<ExtensionDisplayInfo>), typeof(EnabledExtensionsSelector), new PropertyMetadata(null));

        public AsyncObservableCollection<ExtensionDisplayInfo> EnabledExtensions
        {
            get { return (AsyncObservableCollection<ExtensionDisplayInfo>)GetValue(SelectedAppsProperty); }
            set { SetValue(SelectedAppsProperty, value); }
        }

        public static readonly DependencyProperty SelectedAppsProperty =
            DependencyProperty.Register(nameof(EnabledExtensions), typeof(AsyncObservableCollection<ExtensionDisplayInfo>), typeof(EnabledExtensionsSelector), new PropertyMetadata(null));


        /// <summary>
        /// Indicates that the code in LoadData() is executing
        /// </summary>
        protected bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        protected static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(EnabledExtensionsSelector), new PropertyMetadata(false));


        private void LoadData()
        {
            ExtensionLoader extensionLoader = new ExtensionLoader();
            _allExtensionsCollection.AddRange(extensionLoader.GetAllAvailableExtensions()
                .Select(pei => new ExtensionDisplayInfo(pei)));
        }

        private void OnLoadDataComplete()
        {
            AllExtensions = new AsyncObservableCollection<ExtensionDisplayInfo>(_allExtensionsCollection);
            SetEnabledExtensions();
        }

        private void OkButtonHandler()
        {
            EnabledExtensionsChanged?.Invoke(this, new EnabledExtensionsEventArgs(EnabledExtensions
                .Select(ee => ee.PersistableExtensionInfo)));
        }

        private void CancelButtonHandler()
        {
            EnabledExtensionsChanged?.Invoke(this, new EnabledExtensionsEventArgs(null));
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            AllExtensions.Clear();
            IsLoading = true;
            string searchText = searchTextBox.Text;
            List<ExtensionDisplayInfo> selectedApps = EnabledExtensions.ToList();

            Task.Run(() =>
            {
                IList<ExtensionDisplayInfo> filteredAppList = _allExtensionsCollection
                .Where(sai => sai.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                .Where(sai => !selectedApps.Contains(sai))
                .ToList();

                Dispatcher
                .BeginInvoke(DispatcherPriority.Background, new Action(() => AllExtensions.AddRange(filteredAppList)))
                .Task.ContinueWith((t) => Dispatcher.Invoke(() => IsLoading = false));
            });

        }


        public class ExtensionDisplayInfo : BindableObject
        {
            public PersistableExtensionInfo PersistableExtensionInfo { get; }

            public ExtensionDisplayInfo(PersistableExtensionInfo persistableExtensionInfo)
            {
                PersistableExtensionInfo = persistableExtensionInfo;

                Version = persistableExtensionInfo.Version;
                Author = persistableExtensionInfo.Author;
                Description = persistableExtensionInfo.Description;
            }

            /// <summary>
            /// The version of the extension
            /// </summary>
            private double _version;
            public double Version
            {
                get { return _version; }
                set { SetField(ref _version, value); }
            }

            /// <summary>
            /// Who wrote the extension
            /// </summary>
            private string _author;
            public string Author
            {
                get { return _author; }
                set { SetField(ref _author, value); }
            }

            /// <summary>
            /// The description of the extension
            /// </summary>
            private string _description;
            public string Description
            {
                get { return _description; }
                set { SetField(ref _description, value); }
            }
        }

    }
}
