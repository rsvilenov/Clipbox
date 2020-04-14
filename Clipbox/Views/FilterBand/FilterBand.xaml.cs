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




        private AsyncObservableCollection<SourceApplicationInfo> SelectedApplications
        {
            get { return (AsyncObservableCollection<SourceApplicationInfo>)GetValue(SelectedApplicationsProperty); }
            set { SetValue(SelectedApplicationsProperty, value); }
        }

        private static readonly DependencyProperty SelectedApplicationsProperty =
            DependencyProperty.Register(nameof(SelectedApplications), typeof(AsyncObservableCollection<SourceApplicationInfo>), typeof(FilterBand), new PropertyMetadata(null));



        private string WindowCaptionSearchString
        {
            get { return (string)GetValue(WindowCaptionSearchStringProperty); }
            set { SetValue(WindowCaptionSearchStringProperty, value); }
        }

        private static readonly DependencyProperty WindowCaptionSearchStringProperty =
            DependencyProperty.Register(nameof(WindowCaptionSearchString), typeof(string), typeof(FilterBand), new PropertyMetadata(null));



        private string SelectedAppsString
        {
            get { return (string)GetValue(SelectedAppsStringProperty); }
            set { SetValue(SelectedAppsStringProperty, value); }
        }

        private static readonly DependencyProperty SelectedAppsStringProperty =
            DependencyProperty.Register(nameof(SelectedAppsString), typeof(string), typeof(FilterBand), new PropertyMetadata(string.Empty));



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

            if (CloudExpander.IsExpanded)
            {
                bool areBothSelected = LocalMachineSourceSelected && CloudSourceSelected;

                if (LocalMachineSourceSelected)
                {
                    SourceMachineUniqueIdSearchTerm smUidTerm = new SourceMachineUniqueIdSearchTerm
                        (new List<string> { MachineUtils.GetMachineGuid() })
                    {
                        Occurrence = areBothSelected ?
                            Core.Search.Enums.TermOccurrence.NON_OBLIGATORY :
                            Core.Search.Enums.TermOccurrence.OBLIGATORY
                    };

                    SearchTerms.Add(smUidTerm);
                }

                if (CloudSourceSelected)
                {
                    SourceMachineUniqueIdSearchTerm smUidTerm = new SourceMachineUniqueIdSearchTerm
                        (new List<string> { MachineUtils.GetMachineGuid() })
                    {
                        Occurrence = areBothSelected ?
                            Core.Search.Enums.TermOccurrence.NON_OBLIGATORY :
                            Core.Search.Enums.TermOccurrence.NON_PRESENT
                    };
                    
                    SearchTerms.Add(smUidTerm);
                }

                // hack: if we have added only non-obligatory search terms,
                // the search will not be possible, so we should add
                // some obligatory terms which will not affect the result.
                if (SearchTerms.All(t => t.Occurrence == Core.Search.Enums.TermOccurrence.NON_OBLIGATORY))
                {
                    var allContentClasses = (ClipboardContentClass[])Enum.GetValues(typeof(ClipboardContentClass));
                    SearchTerms.Add(new ClipboardContentTypeSearchTerm(allContentClasses));
                }
            }

            if (DateRangeToggled)
            {
                if (StartDateMs > -1 && EndDateMs > -1)
                {
                    long minUtcTicks = (new DateTime(1970, 1, 1))
                    .AddMilliseconds(StartDateMs)
                    .Ticks;

                    long maxUtcTicks = (new DateTime(1970, 1, 1))
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

        private bool LocalMachineSourceSelected
        {
            get { return (bool)GetValue(LocalMachineSourceSelectedProperty); }
            set { SetValue(LocalMachineSourceSelectedProperty, value); }
        }

        private static readonly DependencyProperty LocalMachineSourceSelectedProperty =
            DependencyProperty.Register(nameof(LocalMachineSourceSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

        private bool CloudSourceSelected
        {
            get { return (bool)GetValue(CloudSourceSelectedProperty); }
            set { SetValue(CloudSourceSelectedProperty, value); }
        }

        private static readonly DependencyProperty CloudSourceSelectedProperty =
            DependencyProperty.Register(nameof(CloudSourceSelected), typeof(bool), typeof(FilterBand),
                new PropertyMetadata(false, (d, p) => RebuildSearchTermCollection(d)));

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

        private void CloudExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private void UncheckChildToggleButtons(Expander sender)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    
                    List<UIElement> toggleButtons = Traversing.GetVisualChildCollection(sender, (child) => child is ToggleButton);

                    // do not allow every unchecking to trigger rebuilding of the
                    // search term collection and firing the SearchTermsChanged event
                    _suppressRebuildSearchTermCollection = true;

                    foreach (UIElement child in toggleButtons)
                    {
                        if (child is ToggleButton tglBtn && tglBtn.IsChecked == true)
                        {
                            tglBtn.IsChecked = false;
                        }
                    }

                    _suppressRebuildSearchTermCollection = false;

                    // when all ui state modifications are complete,
                    // rebuild the collection and fire SearchTermsChanged
                    RebuildSearchTermCollection();
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

                        StartDateMs = dialogArgs.StartMs;
                        EndDateMs = dialogArgs.EndMs;

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
    }
}
