using Clipbox.Core.Extensibility.Contracts;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.Interfaces
{
    public interface IClipboardItemsDataProvider
    {
        IReadOnlyList<long> GetAllCreationTimestampTicksUtc();

        IReadOnlyDictionary<IApplicationInfo, byte[]> GetAllSourceAppsAndIcons();

        IReadOnlyDictionary<IMachineInfo, string> GetAllSourceMachinesAndUsernames();
    }
}
