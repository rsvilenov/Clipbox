using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public abstract class SearchObjectBase<T> : ISearchObject
    {
        private List<string> _stringValues = new List<string>();

        public IEnumerable<string> StringValues
        {
            get { return _stringValues; }
        }

        private List<T> _values = new List<T>();
        /// <summary>
        /// The values in that collection are expanded this way: value1 OR value2 OR value3...
        /// </summary>
        public IEnumerable<T> Values
        {
            get { return _values; }
        }

        protected void AddValueRange(IEnumerable<T> values)
        {
            _values.AddRange(values);
            _stringValues.AddRange(GetValuesAsStringCollection(values));
        }

        private IEnumerable<string> GetValuesAsStringCollection(IEnumerable<T> values)
        {
            if (typeof(T) == typeof(string))
            {
                return values.Select(sv => sv as string);
            }
            else
            {
                return values.Select(sv => sv.ToString());
            }
        }

        public Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }

        protected abstract SearchCategory SearchCategory { get; }

        public SearchCategory Category
        {
            get
            {
                return SearchCategory;
            }
        }

        public bool Validate()
        {
            return ValidateObject();
        }

        protected virtual bool ValidateObject()
        {
            return Values != null
                && Values.Any()
                && StringValues.Any()
                && StringValues.All(s => !string.IsNullOrWhiteSpace(s));
        }
    }
}
