using Clipbox.Infrastructure.UI.CustomControls;
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
    /// Interaction logic for ExpandableSearchBox.xaml
    /// </summary>
    public partial class ExpandableSearchBox : UserControl
    {


        public double TextBoxExpandedWidth
        {
            get { return (double)GetValue(TextBoxExpandedWidthProperty); }
            set { SetValue(TextBoxExpandedWidthProperty, value); }
        }

        public static readonly DependencyProperty TextBoxExpandedWidthProperty =
            DependencyProperty.Register(nameof(TextBoxExpandedWidth), typeof(double), typeof(ExpandableSearchBox), new PropertyMetadata(200d));



        public ExpandableSearchBox()
        {
            InitializeComponent();
            this.Expander.DataContext = this;
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            string text = ((SearchTextBox)sender).Text;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            searchTextBox.Focus();
        }
    }
}
