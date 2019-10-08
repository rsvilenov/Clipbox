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



        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ExpandableSearchBox), new PropertyMetadata(string.Empty));



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
            SearchText = ((SearchTextBox)sender).Text;

            // fire search events/commands
            SearchRequested.Invoke(this, new SearchRequestEventArgs(SearchText));
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
                SearchText = string.Empty;
                // fire search events/commands
                SearchRequested.Invoke(this, new SearchRequestEventArgs(string.Empty));
            }
        }
    }
}
