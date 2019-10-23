using Clipbox.Core.ClipboardContent.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public abstract class IndexUnitBase<T> : SearchObjectBase<T>, IIndexUnit
    {
        public IndexUnitBase(IClipboardItem clipboardItem)
        {
            Values = GetTermValuesFromClipboardItem(clipboardItem);
            StringValues = GetValuesAsStringCollection(Values);
        }

        public IndexUnitBase(IEnumerable<T> values)
        {
            Values = values.ToList();
            StringValues = GetValuesAsStringCollection(Values);
        }


        protected abstract IEnumerable<T> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem);
    }
}
