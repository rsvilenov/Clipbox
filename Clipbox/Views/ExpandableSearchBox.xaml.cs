using Clipbox.Infrastructure.UI.CustomControls;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for ExpandableSearchBox.xaml
    /// </summary>
    public partial class ExpandableSearchBox : UserControl
    {
        public event EventHandler Expanded;

        public event EventHandler<SearchRequestEventArgs> SearchRequested;

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

            // fire search events/commands
            SearchRequested.Invoke(this, new SearchRequestEventArgs(text));
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Expanded?.Invoke(this, new EventArgs());
            searchTextBox.Focus();
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = string.Empty;
                // fire search events/commands
                SearchRequested.Invoke(this, new SearchRequestEventArgs(string.Empty));
            }
        }
    }
}
