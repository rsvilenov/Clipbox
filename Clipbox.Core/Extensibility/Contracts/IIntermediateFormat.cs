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
        /// This contains informatin for the intermediate format,
        /// such as FormatGuid, ContentClass and IsTransferable
        /// </summary>
        IIntermediateFormatMetadata Metadata { get; }
    }
}
