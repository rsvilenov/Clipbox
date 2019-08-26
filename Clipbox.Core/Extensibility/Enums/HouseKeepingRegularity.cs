using System;

namespace Clipbox.Core.Extensibility.Enums
{
    /// <summary>
    /// Determines when and how often the housekeeping should be performed
    /// </summary>
    [Flags]
    public enum HouseKeepingRegularity
    {
        /// <summary>
        /// Performs the housekeeping upon showing/hiding the clipboard view controls
        /// </summary>
        UponViewClipboardContents,

        /// <summary>
        /// Currently unused.
        /// </summary>
        Hourly,

        /// <summary>
        /// Currently unused.
        /// </summary>
        Daily
    }
}
