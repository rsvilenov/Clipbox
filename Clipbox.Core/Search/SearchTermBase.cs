using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public abstract class SearchTermBase<T> : ISearchTerm
    {
        // this class is notoriously over-engineered...

        public SearchTermBase(IClipboardItem clipboardItem)
        {
            SearchValues = GetTermValuesFromClipboardItem(clipboardItem);
            SearchStringValues = GetValuesAsStringCollection(SearchValues);
        }

        public SearchTermBase(IEnumerable<T> searchValues)
        {
            SearchValues = searchValues.ToList();
            SearchStringValues = GetValuesAsStringCollection(SearchValues);
        }
        
        protected abstract IEnumerable<T> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem);
        
        /// <summary>
        /// The values in that collection are expanded this way: value1 OR value2 OR value3...
        /// </summary>
        public IEnumerable<T> SearchValues { get; private set; }

        public IEnumerable<string> SearchStringValues { get; private set; }

        public SearchCategory Category
        {
            get
            {
                return SearchCategoryForTerm;
            }
        }

        public bool Validate()
        {
            return ValidateTerm();
        }

        protected virtual bool ValidateTerm()
        {
            return SearchValues != null
                && SearchValues.Any()
                && SearchStringValues.Any() 
                && SearchStringValues.All(s => !string.IsNullOrWhiteSpace(s));
        }
        
        protected abstract SearchCategory SearchCategoryForTerm { get; }
        
        protected virtual IEnumerable<string> GetValuesAsStringCollection(IEnumerable<T> searchValues)
        {
            if (typeof(T) == typeof(string))
            {
                return searchValues.Select(sv => sv as string);
            }
            else
            {
                return searchValues.Select(sv => sv.ToString());
            }
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} :  [{ Category }] is in ({string.Join(", ", SearchStringValues ?? Enumerable.Empty<string>() )})";
        }
    }
}
