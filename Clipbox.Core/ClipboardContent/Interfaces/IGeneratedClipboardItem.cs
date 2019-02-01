using System.Collections.Generic;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IGeneratedClipboardItem
    {
        IReadOnlyCollection<IGeneratedClipboardDataObject> ClipboardDataObjects { get; }
    }
}
