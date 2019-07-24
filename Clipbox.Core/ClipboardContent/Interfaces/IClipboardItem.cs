using System.Collections.Generic;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardItem : IHaveClipboardItemInfo
    {
        IList<IClipboardDataObject> ClipboardDataObjects { get; }
        
        bool IsShrinked { get; }
        
        bool IsPinned { get; }

        bool IsTemporary { get; }
    }
}
