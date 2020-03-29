using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Views.EventArgsClasses;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Clipbox.Views
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
            if (!(TypeExpander.IsExpanded || CloudExpander.IsExpanded) && !DateRangeToggled)
            {
                return;
            }

            SearchTerms.Clear();

            if (TextTypeSelected || ImageTypeSelected
                || FileTypeSelected || GeneralTypeSelected)
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

                SearchTerms.Add(new ClipboardContentTypeSearchTerm(contentClasses));
            }

            if (LocalMachineSourceSelected || CloudSourceSelected)
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
            if (SearchTerms.Any())
            {
                SearchTerms.Clear();
                SearchTermsChanged?.Invoke(this, new EventArgs());
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    List<UIElement> toggleButtons = Traversing.GetVisualChildCollection(sender, (child) => child is ToggleButton);
                    foreach (UIElement child in toggleButtons)
                    {
                        if (child is ToggleButton tglBtn)
                        {
                            tglBtn.IsChecked = false;
                        }
                    }
                }));
        }

        private void ToggleDateRangeSelectionButton_Checked(object sender, RoutedEventArgs e)
        {
            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center,
                MinWidth = 450,
                MinHeight = 200
            };


            var dateRangeSelector = new DateRangeSelector
            {
                ClipboardItemsDataProvider = this.ClipboardItemsDataProvider
            };

            popup.Child = dateRangeSelector;

            EventHandler<DateRangeEventArgs> dateRangeSelectedEventHandler = null;

            dateRangeSelectedEventHandler = new EventHandler<DateRangeEventArgs>
                ((_, dialogArgs) => 
            {
                popup.IsOpen = false;
                dateRangeSelector.DateRangeSelected -= dateRangeSelectedEventHandler;

                if (dialogArgs.StartMs > -1 && dialogArgs.EndMs > -1)
                {
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
        }

        
    }
}
