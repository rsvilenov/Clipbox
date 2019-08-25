using System;

namespace Clipbox.Core.Extensibility.Enums
{
    [Flags]
    public enum HouseKeepingRegularity
    {
        UponViewClipboardContents,
        Hourly,
        Daily
    }
}
