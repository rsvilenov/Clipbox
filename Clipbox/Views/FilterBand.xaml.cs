using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        private void RebuildSearchTermCollection()
        {
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
            
            SearchTermsChanged?.Invoke(this, new EventArgs());
            //if (LocalMachineSourceSelected || CloudSourceSelected)
            //{
            //    List<string> sourceMachine = new List<string>();
            //    if (LocalMachineSourceSelected)
            //    {
            //        sourceMachine.Add()
            //    }
            //}
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

        private void TypeExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private void CloudExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            UncheckChildToggleButtons((Expander)sender);
        }

        private static void UncheckChildToggleButtons(Expander sender)
        {
            List<UIElement> toggleButtons = Traversing.GetVisualChildCollection(sender, (child) => child is ToggleButton);
            foreach (UIElement child in toggleButtons)
            {
                var tglBtn = child as ToggleButton;
                if (tglBtn != null)
                {
                    tglBtn.IsChecked = false;
                }
            }
        }
    }
}
