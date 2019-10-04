using Clipbox.Core.ClipboardContent.Enums;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public abstract class OleClientClipboardFormatSupport 
        : DisposableObject, IClipboardFormatSupport
    {
        public virtual ClipboardContentClass ContentClass { get { return ClipboardContentClass.Unknown; } }

        protected abstract FORMATETC GetFormatetc(string format);

        protected abstract object ExtractData(IDataObject dataObject); 

        public abstract IntPtr DataToPtr(object data);
    }
}
