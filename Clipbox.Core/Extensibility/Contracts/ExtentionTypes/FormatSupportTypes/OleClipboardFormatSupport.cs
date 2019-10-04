using Clipbox.Core.ClipboardContent.Enums;
using System.Runtime.InteropServices.ComTypes;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public abstract class OleClipboardFormatSupport 
        : DisposableObject, IClipboardFormatSupport
    {
        public virtual ClipboardContentClass ContentClass { get { return ClipboardContentClass.Unknown; } }

        protected abstract FORMATETC GetFormatetc(string format);

        protected abstract object ExtractData(IDataObject dataObject);
    }
}
