using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.ObjectHelpers;
using Clipbox.Infrastructure.UI;
using Clipbox.Views.Settings.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for EnabledExtensionsSelector.xaml
    /// </summary>
    public partial class EnabledExtensionsSelector : UserControl
    {
        public ICommand EnableExtension { get; }
        public ICommand DisableExtension { get; }
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public event EventHandler<EnabledExtensionsEventArgs> EnabledExtensionsChanged;

        private readonly List<ExtensionDisplayInfo> _enabledExtensionsInternalList;
        public EnabledExtensionsSelector(IEnumerable<PersistableExtensionInfo> enabledExtensions)
        {
            InitializeComponent();

            _enabledExtensionsInternalList = enabledExtensions.
                Select(ee => new ExtensionDisplayInfo(ee))
                .ToList();

            EnabledExtensions = new AsyncObservableCollection<ExtensionDisplayInfo>();
            _allExtensionsCollection = new List<ExtensionDisplayInfo>();

            EnableExtension = new DelegateCommand<ExtensionDisplayInfo>(EnableExtensionHandler);
            DisableExtension = new DelegateCommand<ExtensionDisplayInfo>(DisableExtensionHandler);
            OkClicked = new DelegateCommand(OkButtonHandler);
            CancelClicked = new DelegateCommand(CancelButtonHandler);

            this.mainBorder.DataContext = this;
            Loaded += InstalledApplicationSelector_Loaded;

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
            _enabledExtensionsInternalList.Remove(obj);
            if (_allExtensionsCollection.Contains(obj))
            {
                DisabledExtensions.Add(obj);
            }
        }

        private void EnableExtensionHandler(ExtensionDisplayInfo obj)
        {
            _enabledExtensionsInternalList.Add(obj);
            EnabledExtensions.Add(obj);
            DisabledExtensions.Remove(obj);
        }

        private List<ExtensionDisplayInfo> _allExtensionsCollection;

        public AsyncObservableCollection<ExtensionDisplayInfo> DisabledExtensions
        {
            get { return (AsyncObservableCollection<ExtensionDisplayInfo>)GetValue(DisabledExtensionsProperty); }
            set { SetValue(DisabledExtensionsProperty, value); }
        }

        public static readonly DependencyProperty DisabledExtensionsProperty =
            DependencyProperty.Register(nameof(DisabledExtensions), typeof(AsyncObservableCollection<ExtensionDisplayInfo>), typeof(EnabledExtensionsSelector), new PropertyMetadata(null));

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
            LoadEnabledExtensions();

            DisabledExtensions = new AsyncObservableCollection<ExtensionDisplayInfo>(_allExtensionsCollection.Except(EnabledExtensions));
        }

        private void LoadEnabledExtensions()
        {
            EnabledExtensions.AddRange(_allExtensionsCollection
                            .Where(edi => _enabledExtensionsInternalList.Contains(edi))
                            .ToList());
        }

        private void OkButtonHandler()
        {
            EnabledExtensionsChanged?.Invoke(this, new EnabledExtensionsEventArgs(_enabledExtensionsInternalList
                .Select(ee => ee.PersistableExtensionInfo)));
        }

        private void CancelButtonHandler()
        {
            EnabledExtensionsChanged?.Invoke(this, new EnabledExtensionsEventArgs(null));
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            DisabledExtensions.Clear();
            IsLoading = true;
            string searchText = searchTextBox.Text;

            Task.Run(() =>
            {
                IList<ExtensionDisplayInfo> filteredExtList = _enabledExtensionsInternalList
                .Where(edi => edi.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1
                    || edi.ExtensionName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                .ToList();

                Dispatcher
                .BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        EnabledExtensions.Clear();
                        EnabledExtensions.AddRange(filteredExtList);
                    }))
                .Task.ContinueWith((t) => Dispatcher.Invoke(() => IsLoading = false));
            });

        }


        public class ExtensionDisplayInfo : BindableObject
        {
            public PersistableExtensionInfo PersistableExtensionInfo { get; }

            public ExtensionDisplayInfo(PersistableExtensionInfo persistableExtensionInfo)
            {
                PersistableExtensionInfo = persistableExtensionInfo;
                ExtensionName = Regex.Replace(persistableExtensionInfo.TypeNameOfExtension, @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
                Version = persistableExtensionInfo.Version;
                Author = persistableExtensionInfo.Author;
                Description = persistableExtensionInfo.Description;
            }

            /// <summary>
            /// The name of the extension
            /// </summary>
            private string _extensionName;
            public string ExtensionName
            {
                get { return _extensionName; }
                set { SetField(ref _extensionName, value); }
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

            public override int GetHashCode()
            {
                return GetHashCodeHelper
                    .Start
                    .Append(ExtensionName)
                    .Append(Description)
                    .Append(Version)
                    .Append(PersistableExtensionInfo.AssemblyLocation);
            }

            public override bool Equals(object obj)
            {
                return obj.GetHashCode() == this.GetHashCode();
            }
        }

    }
}
