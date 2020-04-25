using Clipbox.Win32.Native;
using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class ClipboardFormatSupport<T> : DisposableObject,
        IClipboardFormatSupport
        where T : IIntermediateFormat
    {
        /// <summary>
        /// <see cref="IClipboardFormatSupport.PtrToIntermediateFormat(IntPtr)"/>
        /// </summary>
        public IIntermediateFormat PtrToIntermediateFormat(IntPtr hClipboardData)
        {
            return GetIntermediateFormat(hClipboardData);
        }

        protected abstract T GetIntermediateFormat(IntPtr hClipboardData);
        
        /// <summary>
        /// <see cref="IClipboardFormatSupport.IntermediateFormatToPtr(ref IIntermediateFormat)"/>
        /// </summary>
        public IntPtr IntermediateFormatToPtr(IIntermediateFormat intermediateFormat)
        {
            return GetPtr((T)intermediateFormat);
        }

        protected abstract IntPtr GetPtr(T intermediateFormat);

        /// <summary>
        /// <see cref="IClipboardFormatSupport.OnClipboardFailure(IntPtr)(IntPtr)"/>
        /// </summary>
        public virtual void OnClipboardFailure(IntPtr hClipboardData)
        {
            Kernel32.GlobalFree(hClipboardData);
        }
    }
}
