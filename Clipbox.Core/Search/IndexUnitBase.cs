using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search
{
    [Serializable]
    public abstract class IndexUnitBase<T> : SearchObjectBase<T>, 
        IIndexUnit
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

        protected IndexUnitBase(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected abstract IEnumerable<T> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem);
    }
}
