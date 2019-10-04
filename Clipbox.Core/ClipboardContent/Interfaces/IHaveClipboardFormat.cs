using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveClipboardFormat
    {
        /// <summary>
        /// The clipboard format name, as it is registered in Windows
        /// </summary>
        string Format { get; }

        /// <summary>
        /// The general class, to which the content type belongs (e.g. Image, Text, etc...)
        /// </summary>
        ClipboardContentClass ContentClass { get; }
    }
}
