using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveContentClass
    {
        /// <summary>
        /// The general class, to which the content type belongs (e.g. Image, Text, etc...)
        /// </summary>
        ClipboardContentClass ContentClass { get; }
    }
}
