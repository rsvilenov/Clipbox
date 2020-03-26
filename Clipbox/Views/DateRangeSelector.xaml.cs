using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for DateRangeSelector.xaml
    /// </summary>
    public partial class DateRangeSelector : UserControl
    {
        public DateRangeSelector()
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;
        }



        public List<double> DateTimeMsList
        {
            get { return (List<double>)GetValue(DateTimeMsListProperty); }
            set { SetValue(DateTimeMsListProperty, value); }
        }
        
        public static readonly DependencyProperty DateTimeMsListProperty =
            DependencyProperty.Register(nameof(DateTimeMsList), typeof(List<double>), typeof(DateRangeSelector), new PropertyMetadata(default(List<double>)));



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
            DependencyProperty.Register(nameof(SelectedMinDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d));



        public double SelectedMaxDateTimeMs
        {
            get { return (double)GetValue(SelectedMaxDateTimeMsProperty); }
            set { SetValue(SelectedMaxDateTimeMsProperty, value); }
        }
        
        public static readonly DependencyProperty SelectedMaxDateTimeMsProperty =
            DependencyProperty.Register(nameof(SelectedMaxDateTimeMs), typeof(double), typeof(DateRangeSelector), new PropertyMetadata(0d));




    }
}
