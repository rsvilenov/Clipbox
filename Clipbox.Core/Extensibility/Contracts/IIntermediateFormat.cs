using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.Extensibility.Contracts
{
    /// <summary>
    /// Represents the format in which the clipboard data gets
    /// converted by the IClipboardFormatSupport extensions in order to
    /// be made usable by the other extensions in the program.
    /// </summary>
    public interface IIntermediateFormat
    {
        /// <summary>
        /// The unique identifier of the format type. Since the data
        /// may be serialized, this is used to ensure compatibility.
        /// </summary>
        string FormatGuid { get; }

        /// <summary>
        /// Indicates whether the data can have any meaning on another computer.
        /// Filesystem paths (CF_HDROP), preferred drop effect and the like can't be transfered.
        /// </summary>
        bool IsTransferable { get; }

        /// <summary>
        /// Override this to change the class of the contents
        /// </summary>
        ClipboardContentClass ContentClass { get; }
    }
}
