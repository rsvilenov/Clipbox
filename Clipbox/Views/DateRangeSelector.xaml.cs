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
            this.mainGrid.DataContext = this;
        }



        public long MinUtcTimestamp
        {
            get { return (long)GetValue(MinUtcTimestampProperty); }
            set { SetValue(MinUtcTimestampProperty, value); }
        }

        public static readonly DependencyProperty MinUtcTimestampProperty =
            DependencyProperty.Register(nameof(MinUtcTimestamp), typeof(long), typeof(DateRangeSelector), new PropertyMetadata(0L));


        public long MaxUtcTimestamp
        {
            get { return (long)GetValue(MaxUtcTimestampProperty); }
            set { SetValue(MaxUtcTimestampProperty, value); }
        }

        public static readonly DependencyProperty MaxUtcTimestampProperty =
            DependencyProperty.Register(nameof(MaxUtcTimestamp), typeof(long), typeof(DateRangeSelector), new PropertyMetadata(0L));
        
    }
}
