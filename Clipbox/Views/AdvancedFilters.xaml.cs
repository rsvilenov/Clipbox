using Clipbox.Core.Search;
using Clipbox.Core.Search.Enums;
using Clipbox.Infrastructure.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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


        public AsyncObservableCollection<SearchTermFilterItemWrapper> AvailableSearchTerms
        {
            get { return (AsyncObservableCollection<SearchTermFilterItemWrapper>)GetValue(AvailableSearchTermsProperty); }
            set { SetValue(AvailableSearchTermsProperty, value); }
        }

        public static readonly DependencyProperty AvailableSearchTermsProperty =
            DependencyProperty.Register(nameof(AvailableSearchTerms), typeof(AsyncObservableCollection<SearchTermFilterItemWrapper>), typeof(AdvancedFilters), new PropertyMetadata(null));

        
        public bool IsOptionAnySelected
        {
            get { return (bool)GetValue(IsOptionAnySelectedProperty); }
            set { SetValue(IsOptionAnySelectedProperty, value); }
        }

        public static readonly DependencyProperty IsOptionAnySelectedProperty =
            DependencyProperty.Register(nameof(IsOptionAnySelected), typeof(bool), typeof(AdvancedFilters), new PropertyMetadata(false));

        private Dictionary<SearchCategory, Type> _searchCategoryTermTypeMapping;
        private Dictionary<SearchCategory, Type> _searchCategoryValueTypeMapping;

        public AdvancedFilters()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;

            _searchCategoryValueTypeMapping = new Dictionary<SearchCategory, Type>();
            _searchCategoryTermTypeMapping = new Dictionary<SearchCategory, Type>();

            LoadTypeMappings();

            AvailableSearchTerms = new AsyncObservableCollection<SearchTermFilterItemWrapper>
            {
                new SearchTermFilterItemWrapper
                {

                }
            };
        }

        private void LoadTypeMappings()
        {
            Assembly coreAsm = Assembly.GetAssembly(typeof(ISearchTerm));
            IEnumerable<ISearchTerm> searchTermInstances = coreAsm.GetTypes()
                .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(ISearchTerm)))
                .Select(t => (ISearchTerm)Activator.CreateInstance(t));

            foreach (SearchCategory cat in Enum.GetValues(typeof(SearchCategory)))
            {
                ISearchTerm searchTermInstance = searchTermInstances
                    .Where(i => i.Category == cat)
                    .SingleOrDefault();

                if (searchTermInstance == null)
                {
                    throw new InvalidOperationException($"There are either no search terms for " +
                        $"category {cat} or there are more than one term for that category. " +
                        $"Either case it invalid.");
                }

                _searchCategoryValueTypeMapping.Add(cat, searchTermInstance.ValueType);
                _searchCategoryTermTypeMapping.Add(cat, searchTermInstance.GetType());   
            }
        }

        public SearchTermFilterItem GetSearchTermFilterItem(SearchCategory category)
        {
            Type termType = _searchCategoryTermTypeMapping[category];
            Type valueType = _searchCategoryValueTypeMapping[category];

            return new SearchTermFilterItem(category, termType, valueType);
        }
        
        public class SearchTermFilterItemWrapper : BindableObject
        {
            private SearchCategory _selectedSearchCategory;
            public SearchCategory SelectedSearchCategory
            {
                get { return _selectedSearchCategory; }
                set { SetField(ref _selectedSearchCategory, value); }
            }

            private SearchTermFilterItem _searchTermFilterItem;
            public SearchTermFilterItem SearchTermFilterItem
            {
                get { return _searchTermFilterItem; }
                set { SetField(ref _searchTermFilterItem, value); }
            }
        }
        
        public enum MatchOption
        {
            Equals,
            DoesNotEqual,
            Contains,
            DoesNotContain
        }

        public class SearchTermFilterItem : BindableObject
        {
            private readonly SearchCategory _searchCategory;
            private readonly Type _searchTermType;

            public SearchTermFilterItem(SearchCategory searchCategory, 
                Type searchTermType, 
                Type searchValueType)
            {
                SearchValueType = searchValueType;
                SearchTermDisplayText = searchCategory.ToString();

                _searchCategory = searchCategory;
                _searchTermType = searchTermType;
            }

            public ISearchTerm GetSearchTerm()
            {
                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(SearchValueType);

                var listInstance = Activator.CreateInstance(constructedListType);
                constructedListType.GetMethod("Add").Invoke(listInstance, new object[] { SearchValue });

                return (ISearchTerm)Activator.CreateInstance(_searchTermType, new object[] { listInstance });
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

            private object _searchValue;
            public object SearchValue
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
        }
    }
}
