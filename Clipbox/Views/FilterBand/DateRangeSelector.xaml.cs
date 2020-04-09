using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Views.EventArgsClasses;
using Clipbox.Views.FilterBand.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Views.FilterBand
{
    /// <summary>
    /// Interaction logic for DateRangeSelector.xaml
    /// </summary>
    public partial class DateRangeSelector : FilterDialogBase
    {
        public event EventHandler<DateRangeEventArgs> DateRangeSelected;

        public DateRangeSelector()
        {
            InitializeComponent();
        }

        public DateRangeSelector(IClipboardItemsDataProvider clipboardItemsDataProvider)
            : base(clipboardItemsDataProvider)
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;
        }
        
        private List<double> _dateTimeMsCollection;

        private ObservableCollection<double> DateTimeMsCollection
        {
            get { return (ObservableCollection<double>)GetValue(DateTimeMsCollectionProperty); }
            set { SetValue(DateTimeMsCollectionProperty, value); }
        }
        
        private static readonly DependencyProperty DateTimeMsCollectionProperty =
            DependencyProperty.Register(nameof(DateTimeMsCollection), typeof(ObservableCollection<double>), typeof(DateRangeSelector), new PropertyMetadata(null, DateTimeMsCollection_PropertyChanged));

        private static void DateTimeMsCollection_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DateRangeSelector)d;

            var colChangedHandler = new NotifyCollectionChangedEventHandler(
            (o, args) =>
            {
                self.UpdateMinMaxTime();
            });

            if (e.OldValue != null)
            {
                var collection = (INotifyCollectionChanged)e.OldValue;
                collection.CollectionChanged -= colChangedHandler;
            }

            if (e.NewValue != null)
            {
                var collection = (ObservableCollection<double>)e.NewValue;
                collection.CollectionChanged += colChangedHandler;
            }
            
            ((DateRangeSelector)d).UpdateMinMaxTime();
        }
        
        private void UpdateMinMaxTime()
        {
            MinDateTimeMs = DateTimeMsCollection?.Any() == true ? DateTimeMsCollection.Min() : 0;
            MaxDateTimeMs = DateTimeMsCollection?.Any() == true ? DateTimeMsCollection.Max() : 0;

            // SliderTicks is DoubleCollection and does not play well with observable collections when a binding is used
            rangeSlider.SliderTicks = new DoubleCollection(DateTimeMsCollection);
        }

        private double MaxDateTimeMs
        {
            get { return (double)GetValue(MaxDateTimeMsProperty); }
            set { SetValue(MaxDateTimeMsProperty, value); }
        }

        private static readonly DependencyProperty MaxDateTimeMsProperty =
            DependencyProperty.Register(nameof(MaxDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d));

        
        private double MinDateTimeMs
        {
            get { return (double)GetValue(MinDateTimeMsProperty); }
            set { SetValue(MinDateTimeMsProperty, value); }
        }
        
        private static readonly DependencyProperty MinDateTimeMsProperty =
            DependencyProperty.Register(nameof(MinDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d));
        
        
        public double SelectedMinDateTimeMs
        {
            get { return (double)GetValue(SelectedMinDateTimeMsProperty); }
            set { SetValue(SelectedMinDateTimeMsProperty, value); }
        }

        public static readonly DependencyProperty SelectedMinDateTimeMsProperty =
            DependencyProperty.Register(nameof(SelectedMinDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d, SelectedMinDateTimeMs_PropertyChanged));

        private static void SelectedMinDateTimeMs_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DateRangeSelector)d;
            self.AreMinAndMaxSelectedDatesDistinct = (double)e.NewValue != self.SelectedMaxDateTimeMs;
        }

        public double SelectedMaxDateTimeMs
        {
            get { return (double)GetValue(SelectedMaxDateTimeMsProperty); }
            set { SetValue(SelectedMaxDateTimeMsProperty, value); }
        }
        
        public static readonly DependencyProperty SelectedMaxDateTimeMsProperty =
            DependencyProperty.Register(nameof(SelectedMaxDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d, SelectedMaxDateTimeMs_PropertyChanged));

        private static void SelectedMaxDateTimeMs_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DateRangeSelector)d;
            self.AreMinAndMaxSelectedDatesDistinct = (double)e.NewValue != self.SelectedMinDateTimeMs;
        }

        private bool AreMinAndMaxSelectedDatesDistinct
        {
            get { return (bool)GetValue(AreMinAndMaxSelectedDatesDistinctProperty); }
            set { SetValue(AreMinAndMaxSelectedDatesDistinctProperty, value); }
        }
        
        private static readonly DependencyProperty AreMinAndMaxSelectedDatesDistinctProperty =
            DependencyProperty.Register(nameof(AreMinAndMaxSelectedDatesDistinct), typeof(bool), typeof(DateRangeSelector), new PropertyMetadata(false));

        
        protected override void OnOkButton()
        {
            DateRangeSelected?.Invoke(this, new DateRangeEventArgs(SelectedMinDateTimeMs, SelectedMaxDateTimeMs));
        }

        protected override void OnCancelButton()
        {
            // on cancel, return -1 for both ends of the range
            DateRangeSelected?.Invoke(this, new DateRangeEventArgs(-1, -1));
        }

        protected override void LoadData()
        {
            _dateTimeMsCollection = ClipboardItemsDataProvider
                .GetAllCreationTimestampTicksUtc()
                .Distinct()
                .Select(ticks => new DateTime(ticks).Date) // get the utc date
                .Select(date => (double)(date - new DateTime(1970, 1, 1)).TotalMilliseconds)
                .Distinct()
                .ToList();
        }

        protected override void OnLoadDataComplete()
        {
            DateTimeMsCollection = new ObservableCollection<double>(_dateTimeMsCollection);
            SelectedMinDateTimeMs = DateTimeMsCollection?.Min() ?? 0;
            SelectedMaxDateTimeMs = DateTimeMsCollection?.Max() ?? 0;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // When the popup gets wider due to changes of the text length inside,
            // do not let it shrink back when the text becomes shorter;
            // This prevents the impression that the window is flickering
            // when the user changes the start and end dates and the text
            // overflows the current width of the window.
            if (this.ActualWidth > this.MinWidth)
            {
                this.MinWidth = this.ActualWidth;
            }

            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
