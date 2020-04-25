using Clipbox.Core.ClipboardContent.Enums;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    /// <summary>
    /// TODO: make these OLE types inherit from a common type along with IClipboardFormaSupport types
    /// </summary>
    public abstract class OleClientClipboardFormatSupport 
        : DisposableObject
    {
        public virtual ClipboardContentClass ContentClass { get { return ClipboardContentClass.Unknown; } }

        protected abstract FORMATETC GetFormatetc(string format);

        protected abstract object ExtractData(IDataObject dataObject); 

        public abstract IntPtr DataToPtr(object data);
    }
}
