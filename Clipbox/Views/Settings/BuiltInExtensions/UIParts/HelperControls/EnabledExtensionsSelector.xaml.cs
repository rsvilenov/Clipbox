using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.ObjectHelpers;
using Clipbox.Infrastructure.UI;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls.BindableObjects;
using Clipbox.Views.Settings.EventArgsClasses;
using Clipbox.Views.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls
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


        private void DisableExtensionHandler(ExtensionDisplayInfo extDisplayInfo)
        {
            IEnumerable<ExtensionDisplayInfo> dependentExtensions =  CollectDependentExtensions(extDisplayInfo);
            if (dependentExtensions.Any())
            {
                OpenConfirmationDialog(extDisplayInfo, dependentExtensions, isDisable: true);
            }
            else
            {
                EnableOrDisableExtension(extDisplayInfo, isDisable: true);
            }
        }
        private void EnableExtensionHandler(ExtensionDisplayInfo extDisplayInfo)
        {
            IEnumerable<ExtensionDisplayInfo> dependentExtensions = CollectDependentExtensions(extDisplayInfo);
            if (dependentExtensions.Any())
            {
                OpenConfirmationDialog(extDisplayInfo, dependentExtensions, isDisable: false);
            }
            else
            {
                EnableOrDisableExtension(extDisplayInfo, isDisable: false);
            }
        }

        private void EnableOrDisableExtension(ExtensionDisplayInfo edi, bool isDisable)
        {
            if (isDisable)
            {
                EnabledExtensions.Remove(edi);
                _enabledExtensionsInternalList.Remove(edi);
                if (_allExtensionsCollection.Contains(edi))
                {
                    DisabledExtensions.Add(edi);
                }
            }
            else
            {
                _enabledExtensionsInternalList.Add(edi);
                EnabledExtensions.Add(edi);
                DisabledExtensions.Remove(edi);
            }
        }

        private IEnumerable<ExtensionDisplayInfo> CollectDependentExtensions(ExtensionDisplayInfo extDisplayInfo)
        {
            SettingsWindow settingsWindow = (SettingsWindow)Window.GetWindow(this);
            if (settingsWindow is ICanResolveDependentExtensions dependencyResolver)
            {
                IEnumerable<Type> dependentTypes = dependencyResolver.
                    GetDependentExtensions(extDisplayInfo.PersistableExtensionInfo.ExtensionObjectTypeFullName);
                return _allExtensionsCollection
                    .Where(edi => dependentTypes
                        .Any(dt => dt.FullName == edi.PersistableExtensionInfo.ExtensionObjectTypeFullName));
            }
            else
            {
                throw new InvalidOperationException($"{nameof(SettingsWindow)} does not implement {nameof(ICanResolveDependentExtensions)}.");
            }
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



        /// <summary>
        /// Used instead of the default IsEnabled, in order to avoid the "disabled" effects from the
        /// default control templates. I don't want to override the templates in order to get rid of
        /// the effects. I just want a semi-transparent black viel on top of everything. :)
        /// </summary>
        public bool IsControlEnabled
        {
            get { return (bool)GetValue(IsControlEnabledProperty); }
            set { SetValue(IsControlEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsControlEnabledProperty =
            DependencyProperty.Register(nameof(IsControlEnabled), typeof(bool), typeof(EnabledExtensionsSelector), new PropertyMetadata(true));



        private void LoadData()
        {
            ExtensionLoader extensionLoader = new ExtensionLoader();
            _allExtensionsCollection.AddRange(extensionLoader.GetAllAvailableExtensions()
                .Select(pei => new ExtensionDisplayInfo(pei.PersistableExtensionInfo)));
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

        private void OpenConfirmationDialog(ExtensionDisplayInfo mainExtDisplayInfo, IEnumerable<ExtensionDisplayInfo> dependentExtensions, bool isDisable)
        {
            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center,
                MinWidth = 450,
                MinHeight = 200
            };


            DepencentExtensionConfirmation confirmDialog = 
                new DepencentExtensionConfirmation(dependentExtensions, isDisablingConfirmation: isDisable);

            popup.Child = confirmDialog;


            EventHandler<ConfirmationEventArgs> confirmDialogAnsweredEventHandler = null;

            confirmDialogAnsweredEventHandler = new EventHandler<ConfirmationEventArgs>
                ((_, dialogArgs) =>
                {
                    // re-enable the main popup (this)
                    this.IsControlEnabled = true;

                    popup.IsOpen = false;
                    confirmDialog.DialogAnswered -= confirmDialogAnsweredEventHandler;

                    if (dialogArgs.IsConfirmed)
                    {
                        foreach(ExtensionDisplayInfo edi in dependentExtensions)
                        {
                            EnableOrDisableExtension(edi, isDisable);
                        }

                        EnableOrDisableExtension(mainExtDisplayInfo, isDisable);
                    }

                });

            confirmDialog.DialogAnswered += confirmDialogAnsweredEventHandler;

            popup.IsOpen = true;

            // disable the main popup (this) while the confirmation popup is open
            this.IsControlEnabled = false;
        }
    }
}
