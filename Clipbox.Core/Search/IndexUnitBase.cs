using Clipbox.Core.ClipboardContent.Interfaces;
using System.Collections.Generic;

namespace Clipbox.Core.Search
{
    public abstract class IndexUnitBase<T> : SearchObjectBase<T>, IIndexUnit
    {
        public IndexUnitBase(IClipboardItem clipboardItem)
        {
            IEnumerable<T> values = GetTermValuesFromClipboardItem(clipboardItem);
            AddValueRange(values);
        }

        public IndexUnitBase(IEnumerable<T> values)
        {
            AddValueRange(values);
        }


        protected abstract IEnumerable<T> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem);
    }
}
