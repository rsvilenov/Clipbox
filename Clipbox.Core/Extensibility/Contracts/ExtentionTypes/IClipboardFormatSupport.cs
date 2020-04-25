using System;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public interface IClipboardFormatSupport : 
        IClipboxExtension, IDisposable
    {
        /// <summary>
        /// Gets the data from the pointer, obtained by GetClipboardData.
        /// </summary>
        /// <param name="hClipboardData"></param>
        /// <returns></returns>
        IIntermediateFormat PtrToIntermediateFormat(IntPtr hClipboardData);

        /// <summary>
        /// Get a memory pointer from the data. The data can sometimes undergo changes,
        /// or it may be replaced by an entirely different object (such is the case with Metafile, 
        /// which has a method GetHenhmetafile, which invalidates the object...).
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        IntPtr IntermediateFormatToPtr(IIntermediateFormat data);

        /// <summary>
        /// Allows us to decide what to do with the handle if SetClipboardData fails.
        /// SetClipboardData expects hGlobal for most clipboard formats, however, 
        /// this is not the case for some formats such as Bitmap and Enhanced Metafile.
        /// For these formats, we have to override this method and implement the
        /// appropriate cleanup code.
        /// </summary>
        void OnClipboardFailure(IntPtr hClipboardData);
    }
}
