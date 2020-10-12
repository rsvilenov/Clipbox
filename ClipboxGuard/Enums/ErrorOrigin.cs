using System;
using System.ComponentModel;

namespace ClipboxGuard.Enums
{
    [Flags]
    public enum ErrorOrigin
    {
        [Description("Main process")]
        MainProcess = 1,

        [Description("Search process")]
        SearchProcess = 2,

        [Description("Search index")]
        SearchIndex = 4,

        [Description("Guard process")]
        GuardProcess = 8,

        [Description("Deskband")]
        Deskband = 16,

        [Description("DeskbandOwner")]
        DeskbandOwner = 32,

        [Description("Unknown")]
        Unknown = 64
    }
}
