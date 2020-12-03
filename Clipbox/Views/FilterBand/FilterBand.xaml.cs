using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Views.FilterBand.EventArgsClasses;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Views.FilterBand
{
    /// <summary>
    /// Interaction logic for FilterBand.xaml
    /// </summary>
    public partial class FilterBand : UserControl
    {
        public FilterBand()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        private bool _suppressRebuildSearchTermCollection;

        public event EventHandler SearchTermsChanged;

        private static void RebuildSearchTermCollection(DependencyObject d)
        {
            ((FilterBand)d).RebuildSearchTermCollection();
        }

        
        public IClipboardItemsDataProvider ClipboardItemsDataProvider
        {
            get { return (IClipboardItemsDataProvider)GetValue(ClipboardItemsDataProviderProperty); }
            set { SetValue(ClipboardItemsDataProviderProperty, value); }
        }

        public static readonly DependencyProperty ClipboardItemsDataProviderProperty =
            DependencyProperty.Register(nameof(ClipboardItemsDataProvider), typeof(IClipboardItemsDataProvider), typeof(FilterBand), new PropertyMetadata(null));
        

        public AsyncObservableCollection<string> SourceMachineUniqueIds
        {
            get { return (AsyncObservableCollection<string>)GetValue(SourceMachineUniqueIdsProperty); }
            set { SetValue(SourceMachineUniqueIdsProperty, value); }
        }

        public static readonly DependencyProperty SourceMachineUniqueIdsProperty =
            DependencyProperty.Register(nameof(SourceMachineUniqueIds), typeof(AsyncObservableCollection<string>), typeof(FilterBand), new PropertyMetadata(new AsyncObservableCollection<string>()));



        public bool ShouldParentBeEnabled
        {
            get { return (bool)GetValue(ShouldParentBeEnabledProperty); }
            set { SetValue(ShouldParentBeEnabledProperty, value); }
        }
        
        public static readonly DependencyProperty ShouldParentBeEnabledProperty =
            DependencyProperty.Register(nameof(ShouldParentBeEnabled), typeof(bool), typeof(FilterBand), new PropertyMetadata(true));


        public bool IsCloudSyncServiceEnabled
        {
            get { return (bool)GetValue(IsCloudSyncServiceEnabledProperty); }
            set { SetValue(IsCloudSyncServiceEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsCloudSyncServiceEnabledProperty =
            DependencyProperty.Register(nameof(IsCloudSyncServiceEnabled), typeof(bool), typeof(FilterBand), new PropertyMetadata(false));


        private AsyncObservableCollection<SourceApplicationInfo> SelectedApplications
        {
            get { return (AsyncObservableCollection<SourceApplicationInfo>)GetValue(SelectedApplicationsProperty); }
            set { SetValue(SelectedApplicationsProperty, value); }
        }

        private static readonly DependencyProperty SelectedApplicationsProperty =
            DependencyProperty.Register(nameof(SelectedApplications), typeof(AsyncObservableCollection<SourceApplicationInfo>), typeof(FilterBand), new PropertyMetadata(null));


        private string SelectedAppsString
        {
            get { return (string)GetValue(SelectedAppsStringProperty); }
            set { SetValue(SelectedAppsStringProperty, value); }
        }

        private static readonly DependencyProperty SelectedAppsStringProperty =
            DependencyProperty.Register(nameof(SelectedAppsString), typeof(string), typeof(FilterBand), new PropertyMetadata(string.Empty));


        private string WindowCaptionSearchString
        {
            get { return (string)GetValue(WindowCaptionSearchStringProperty); }
            set { SetValue(WindowCaptionSearchStringProperty, value); }
        }

        private static readonly DependencyProperty WindowCaptionSearchStringProperty =
            DependencyProperty.Register(nameof(WindowCaptionSearchString), typeof(string), typeof(FilterBand), new PropertyMetadata(null));
        
        private AsyncObservableCollection<SourceMachineInfo> SelectedMachines
        {
            get { return (AsyncObservableCollection<SourceMachineInfo>)GetValue(SelectedMachinesProperty); }
            set { SetValue(SelectedMachinesProperty, value); }
        }

        private static readonly DependencyProperty SelectedMachinesProperty =
            DependencyProperty.Register(nameof(SelectedMachines), typeof(AsyncObservableCollection<SourceMachineInfo>), typeof(FilterBand), new PropertyMetadata(null));

        private string SelectedMachinesString
        {
            get { return (string)GetValue(SelectedMachinesStringProperty); }
            set { SetValue(SelectedMachinesStringProperty, value); }
        }

        private static readonly DependencyProperty SelectedMachinesStringProperty =
            DependencyProperty.Register(nameof(SelectedMachinesString), typeof(string), typeof(FilterBand), new PropertyMetadata(string.Empty));
        

        private double StartDateMs
        {
            get { return (double)GetValue(StartDateMsProperty); }
            set { SetValue(StartDateMsProperty, value); }
        }

        private static readonly DependencyProperty StartDateMsProperty =
            DependencyProperty.Register(nameof(StartDateMs), typeof(double), typeof(FilterBand), new PropertyMetadata(-1d));

        
        private double EndDateMs
        {
            get { return (double)GetValue(EndDateMsProperty); }
            set { SetValue(EndDateMsProperty, value); }
        }


        public bool AreStartAndEndDateTheSameDay
        {
            get { return (bool)GetValue(AreStartAndEndDateTheSameDayProperty); }
            set { SetValue(AreStartAndEndDateTheSameDayProperty, value); }
        }

        public static readonly DependencyProperty AreStartAndEndDateTheSameDayProperty =
            DependencyProperty.Register(nameof(AreStartAndEndDateTheSameDay), typeof(bool), typeof(FilterBand), new PropertyMetadata(false));



        private static readonly DependencyProperty EndDateMsProperty =
            DependencyProperty.Register(nameof(EndDateMs), typeof(double), typeof(FilterBand), new PropertyMetadata(-1d));

        
        public List<double> UniqueDatesOfItemsMs { get; private set; }
        
        public void SetUniqueDatesOfItemsMs(IEnumerable<double> datesMs)
        {
            UniqueDatesOfItemsMs = new List<double>(datesMs);
        }

        private void RebuildSearchTermCollection()
        {
            if (_suppressRebuildSearchTermCollection)
            {
                return;
            }

            SearchTerms.Clear();

            if (TypeExpander.IsExpanded)
            {
                List<ClipboardContentClass> contentClasses = new List<ClipboardContentClass>();
                if (TextTypeSelected)
                {
                    contentClasses.Add(ClipboardContentClass.Text);
                }
                if (ImageTypeSelected)
                {
                    contentClasses.Add(ClipboardContentClass.Image);
                }
                if (FileTypeSelected)
                {
                    contentClasses.Add(ClipboardContentClass.File);
                }
                if (GeneralTypeSelected)
                {
                    contentClasses.Add(ClipboardContentClass.GeneralContent);
                }

                if (contentClasses.Any())
                {
                    SearchTerms.Add(new ClipboardContentTypeSearchTerm(contentClasses));
                }
            }

            if (SourceMachineExpander.IsExpanded)
            {
                List<string> userNameMachineIdPairs = new List<string>();
                if (LocalMachineSourceToggled)
                {
                    userNameMachineIdPairs.Add($"{Environment.UserName}@{MachineUtils.GetMachineGuid()}");
                }

                if (RemoteMachineSourceToggled)
                {
                    userNameMachineIdPairs.AddRange(SelectedMachines.Select(sm => $"{sm.UserName}@{sm.MachineUniqueId}"));
                }

                if (userNameMachineIdPairs.Any())
                {
                    SearchTerms.Add(new SourceMachineIdUserNamePairSearchTerm(userNameMachineIdPairs));
                }
            }

            if (DateRangeToggled)
            {
                if (StartDateMs > -1 && EndDateMs > -1)
                {
                    long minUtcTicks = (new DateTime(1970, 1, 1))
                    .ToUniversalTime()
                    .AddMilliseconds(StartDateMs)
                    .Ticks;

                    long maxUtcTicks = (new DateTime(1970, 1, 1))
                        .ToUniversalTime()
                        .AddMilliseconds(EndDateMs)
                        .AddDays(1)
                        .Ticks;

                    SearchTerms.Add(new MinCreationDateSearchTerm(new List<long>
                    {
                        minUtcTicks
                    }));

                    SearchTerms.Add(new MaxCreationDateSearchTerm(new List<long>
                    {
                        maxUtcTicks
                    }));
                }
            }
            else
            {
                StartDateMs = -1;
                EndDateMs = -1;
            }

            if (AppSelectorToggled)
            {
                if (!string.IsNullOrWhiteSpace(WindowCaptionSearchString))
                {
                    SearchTerms.Add(new SourceWindowCaptionSearchTerm(new List<string>
                    {
                        WindowCaptionSearchString
                    }));
                }

                if (SelectedApplications.Any())
                {
                    SearchTerms.Add(new SourceApplicationNameSearchTerm(
                        SelectedApplications
                        .Select(sa => sa.ExecutableName)));
                }
            }

            SearchTermsChanged?.Invoke(this, new EventArgs());
        }

        public AsyncObservableCollection<ISearchTerm> SearchTerms
        {
            get { return (AsyncObservableCollection<ISearchTerm>)GetValue(SearchTermsProperty); }
            set { SetValue(SearchTermsProperty, value); }
        }

        public static readonly DependencyProperty SearchTermsProperty =
            DependencyProperty.Register(nameof(SearchTerms), typeof(AsyncObservableCollection<ISearchTerm>), typeof(FilterBand), new PropertyMetadata(new AsyncObservableCollection<ISearchTerm>()));

        private bool TextTypeSelected
        {
            get { return (bool)GetValue(TextTypeSelectedProperty); }
            set { SetValue(TextTypeSelectedProperty, value); }
        }

        private static readonly DependencyProperty TextTypeSelectedProperty =
            DependencyProperty.Register(nameof(TextTypeSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool FileTypeSelected
        {
            get { return (bool)GetValue(FileTypeSelectedProperty); }
            set { SetValue(FileTypeSelectedProperty, value); }
        }

        private static readonly DependencyProperty FileTypeSelectedProperty =
            DependencyProperty.Register(nameof(FileTypeSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool ImageTypeSelected
        {
            get { return (bool)GetValue(ImageTypeSelectedProperty); }
            set { SetValue(ImageTypeSelectedProperty, value); }
        }

        private static readonly DependencyProperty ImageTypeSelectedProperty =
            DependencyProperty.Register(nameof(ImageTypeSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool GeneralTypeSelected
        {
            get { return (bool)GetValue(GeneralTypeSelectedProperty); }
            set { SetValue(GeneralTypeSelectedProperty, value); }
        }

        private static readonly DependencyProperty GeneralTypeSelectedProperty =
            DependencyProperty.Register(nameof(GeneralTypeSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool LocalMachineSourceToggled
        {
            get { return (bool)GetValue(LocalMachineSourceToggledProperty); }
            set { SetValue(LocalMachineSourceToggledProperty, value); }
        }

        private static readonly DependencyProperty LocalMachineSourceToggledProperty =
            DependencyProperty.Register(nameof(LocalMachineSourceToggled), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool RemoteMachineSourceToggled
        {
            get { return (bool)GetValue(RemoteMachineSourceToggledProperty); }
            set { SetValue(RemoteMachineSourceToggledProperty, value); }
        }

        private static readonly DependencyProperty RemoteMachineSourceToggledProperty =
            DependencyProperty.Register(nameof(RemoteMachineSourceToggled), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) =>
                {
                    if (!(bool)p.NewValue)
                    {
                        // here we invoke the rebuilding only upon unchecking;
                        // upon checking the rebuilding is invoked by the Checked event handler of the toggle button.
                        RebuildSearchTermCollection(d);
                    }
                }));

        private bool DateRangeToggled
        {
            get { return (bool)GetValue(DateRangeToggledProperty); }
            set { SetValue(DateRangeToggledProperty, value); }
        }

        private static readonly DependencyProperty DateRangeToggledProperty =
            DependencyProperty.Register(nameof(DateRangeToggled), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) =>
                {
                    if (!(bool)p.NewValue)
                    {
                        // here we invoke the rebuilding only upon unchecking;
                        // upon checking the rebuilding is invoked by the Checked event handler of the toggle button.
                        RebuildSearchTermCollection(d);
                    }
                }));

        private bool AppSelectorToggled
        {
            get { return (bool)GetValue(AppSelectorToggledProperty); }
            set { SetValue(AppSelectorToggledProperty, value); }
        }

        private static readonly DependencyProperty AppSelectorToggledProperty =
            DependencyProperty.Register(nameof(AppSelectorToggled), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) =>
                {
                    if (!(bool)p.NewValue)
                    {
                        // here we invoke the rebuilding only upon unchecking;
                        // upon checking the rebuilding is invoked by the Checked event handler of the toggle button.
                        RebuildSearchTermCollection(d);
                    }
                }));

        private void TypeExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private void SourceMachineExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private void UncheckChildToggleButtons(Expander sender)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    
                    List<UIElement> toggleButtons = Traversing.GetVisualChildCollection(sender, (child) => child is ToggleButton);

                    // do not allow every unchecking to trigger rebuilding of the
                    // search term collection and firing the SearchTermsChanged event
                    _suppressRebuildSearchTermCollection = true;

                    bool wereAnyTglButtonsChecked = false;

                    foreach (UIElement child in toggleButtons)
                    {
                        if (child is ToggleButton tglBtn && tglBtn.IsChecked == true)
                        {
                            if (tglBtn.IsChecked == true)
                            {
                                tglBtn.IsChecked = false;
                                wereAnyTglButtonsChecked = true;
                            }
                        }
                    }

                    _suppressRebuildSearchTermCollection = false;

                    if (wereAnyTglButtonsChecked)
                    {
                        // wait for the ui state modifications (animations, etc.) to complete.
                        // TODO: devise a better way of awaiting the animation.
                        Task.Delay(500)
                        .ContinueWith((t) =>
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => RebuildSearchTermCollection())));
                    }
                }));
        }

        private void ToggleDateRangeSelectionButton_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void ToggleSourceWindowButton_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void ToggleDateRangeSelectionButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DateRangeToggled)
            {
                return;
            }
            else
            {
                // do not allow the toggle button to become checked. 
                // we will doggle it by setting DateRangeToggled to true when the user clicks OK.
                e.Handled = true;
            }

            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center,
                MinWidth = 450,
                MinHeight = 200
            };


            var dateRangeSelector = new DateRangeSelector(this.ClipboardItemsDataProvider);

            popup.Child = dateRangeSelector;

            EventHandler<DateRangeEventArgs> dateRangeSelectedEventHandler = null;

            dateRangeSelectedEventHandler = new EventHandler<DateRangeEventArgs>
                ((_, dialogArgs) =>
                {
                    popup.IsOpen = false;
                    ShouldParentBeEnabled = true;
                    dateRangeSelector.DateRangeSelected -= dateRangeSelectedEventHandler;

                    if (dialogArgs.StartMs > -1 && dialogArgs.EndMs > -1)
                    {
                        DateRangeToggled = true;

                        // the values, received from DateRangeEventArgs are UTC
                        StartDateMs = ToLocalTimeMilliseconds(dialogArgs.StartMs);
                        EndDateMs = ToLocalTimeMilliseconds(dialogArgs.EndMs);

                        DateTime minDt = (new DateTime(1970, 1, 1))
                        .AddMilliseconds(StartDateMs);
                        DateTime maxDt = (new DateTime(1970, 1, 1))
                            .AddMilliseconds(EndDateMs);

                        AreStartAndEndDateTheSameDay = minDt.Date == maxDt.Date;

                        RebuildSearchTermCollection();
                    }
                    else
                    {
                        StartDateMs = -1;
                        EndDateMs = -1;

                        DateRangeToggled = false;
                    }
                });

            dateRangeSelector.DateRangeSelected += dateRangeSelectedEventHandler;

            popup.IsOpen = true;
            ShouldParentBeEnabled = false;
        }

        private double ToLocalTimeMilliseconds(double utcMilliseconds)
        {
            DateTime utcDt = (new DateTime(1970, 1, 1))
                .AddMilliseconds(utcMilliseconds)
                .ToLocalTime();
            

            return (double)(utcDt - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private void ToggleApplicationSelectionButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AppSelectorToggled)
            {
                return;
            }
            else
            {
                // do not allow the toggle button to become checked. 
                // we will doggle it by setting AppSelectorToggled to true when the user clicks OK.
                e.Handled = true;
            }

            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center
            };


            var applicationSelector = new ApplicationSelector(this.ClipboardItemsDataProvider);

            popup.Child = applicationSelector;

            EventHandler<ApplicationInfoEventArgs> appInfoSelectedEventHandler = null;

            appInfoSelectedEventHandler = new EventHandler<ApplicationInfoEventArgs>
                ((_, dialogArgs) =>
                {
                    popup.IsOpen = false;
                    ShouldParentBeEnabled = true;
                    applicationSelector.AppInfoSelected -= appInfoSelectedEventHandler;

                    if (string.IsNullOrWhiteSpace(dialogArgs.CaptionSearchString) && (dialogArgs.SelectedApplications == null || !dialogArgs.SelectedApplications.Any()))
                    {
                        AppSelectorToggled = false;
                    }
                    else
                    {
                        AppSelectorToggled = true;

                        WindowCaptionSearchString = dialogArgs.CaptionSearchString;
                        SelectedApplications = new AsyncObservableCollection<SourceApplicationInfo>(dialogArgs.SelectedApplications);
                        SelectedAppsString = string.Join("; ", dialogArgs.SelectedApplications.Select(ai => ai.FormattedProductName));
                        RebuildSearchTermCollection();
                    }
                });

            applicationSelector.AppInfoSelected += appInfoSelectedEventHandler;

            popup.IsOpen = true;
            ShouldParentBeEnabled = false;
        }

        private void ToggleRemoteDeviceSelectionButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RemoteMachineSourceToggled)
            {
                return;
            }
            else
            {
                // do not allow the toggle button to become checked. 
                // we will doggle it by setting AppSelectorToggled to true when the user clicks OK.
                e.Handled = true;
            }

            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center
            };


            var remoteMachineSelector = new RemoteMachineSelector(this.ClipboardItemsDataProvider);

            popup.Child = remoteMachineSelector;

            EventHandler<SourceMachineEventArgs> machineInfoSelectedEventHandler = null;

            machineInfoSelectedEventHandler = new EventHandler<SourceMachineEventArgs>
                ((_, dialogArgs) =>
                {
                    popup.IsOpen = false;
                    ShouldParentBeEnabled = true;
                    remoteMachineSelector.SourceMachinesSelected -= machineInfoSelectedEventHandler;

                    if (dialogArgs.SourceMachines == null || !dialogArgs.SourceMachines.Any())
                    {
                        RemoteMachineSourceToggled = false;
                    }
                    else
                    {
                        RemoteMachineSourceToggled = true;
                        
                        SelectedMachines = new AsyncObservableCollection<SourceMachineInfo>(dialogArgs.SourceMachines);
                        SelectedMachinesString = string.Join("; ", dialogArgs.SourceMachines.Select(sm => $"{sm.UserName}@{sm.MachineName}" ));
                        RebuildSearchTermCollection();
                    }
                });

            remoteMachineSelector.SourceMachinesSelected += machineInfoSelectedEventHandler;

            popup.IsOpen = true;
            ShouldParentBeEnabled = false;
        }
    }
}
