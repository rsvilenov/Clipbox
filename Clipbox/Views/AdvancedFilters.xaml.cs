using Clipbox.Core.Search;
using Clipbox.Core.Search.Enums;
using Clipbox.Infrastructure.UI;
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
    /// Interaction logic for AdvancedFilters.xaml
    /// </summary>
    public partial class AdvancedFilters : UserControl
    {


        public AsyncObservableCollection<SearchTermFilterItem> AvailableSearchTerms
        {
            get { return (AsyncObservableCollection<SearchTermFilterItem>)GetValue(AvailableSearchTermsProperty); }
            set { SetValue(AvailableSearchTermsProperty, value); }
        }

        public static readonly DependencyProperty AvailableSearchTermsProperty =
            DependencyProperty.Register(nameof(AvailableSearchTerms), typeof(AsyncObservableCollection<SearchTermFilterItem>), typeof(AdvancedFilters), new PropertyMetadata(null));

        
        public bool IsOptionAnySelected
        {
            get { return (bool)GetValue(IsOptionAnySelectedProperty); }
            set { SetValue(IsOptionAnySelectedProperty, value); }
        }

        public static readonly DependencyProperty IsOptionAnySelectedProperty =
            DependencyProperty.Register(nameof(IsOptionAnySelected), typeof(bool), typeof(bool), new PropertyMetadata(false));



        public AdvancedFilters()
        {
            InitializeComponent();
        }

        private void LoadAvailableSearchTerms()
        {
            foreach (SearchCategory cat in Enum.GetValues(typeof(SearchCategory)))
            {
                //AvailableSearchTerms.Add(new SearchTermFilterItem
                //{
                //});
            }
        }

        public class SearchTermFilterItem : BindableObject
        {
            ISearchTerm _searchTerm;
 
            public SearchTermFilterItem(SearchCategory searchCategory)
            {
                //_searchTerm = 
            }

            private Type _searchValueType;
            public Type SearchValueType
            {
                get { return _searchValueType; }
                set { SetField(ref _searchValueType, value); }
            }

            private string _searchTermDisplayText;
            public string SearchTermDisplayText
            {
                get { return _searchTermDisplayText; }
                set { SetField(ref _searchTermDisplayText, value); }
            }

            private string _searchValue;
            public string SearchValue
            {
                get { return _searchValue; }
                set { SetField(ref _searchValue, value); }
            }

            private MatchOption _selectedMatchOption;
            public MatchOption SelectedMatchOption
            {
                get { return _selectedMatchOption; }
                set { SetField(ref _selectedMatchOption, value); }
            }

            public enum MatchOption
            {
                Equals,
                DoesNotEqual,
                Contains,
                DoesNotContain
            }


        }
    }
}
