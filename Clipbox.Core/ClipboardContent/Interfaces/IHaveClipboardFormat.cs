using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveClipboardFormat
    {
        /// <summary>
        /// The clipboard format name, as it is registered in Windows
        /// </summary>
        string ClipboardFormatName { get; }
    }
}
