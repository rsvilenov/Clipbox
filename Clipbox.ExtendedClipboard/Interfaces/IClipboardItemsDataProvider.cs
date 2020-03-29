using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.Interfaces
{
    public interface IClipboardItemsDataProvider
    {
        IReadOnlyList<long> GetAllCreationTimestampTicksUtc();

        IReadOnlyDictionary<string, byte[]> GetAllSourceAppsAndIcons();
    }
}
