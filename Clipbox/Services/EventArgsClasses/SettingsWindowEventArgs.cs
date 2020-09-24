using Clipbox.Core.Extensibility.Enums;
using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class SettingsWindowEventArgs : EventArgs
    {
        public SettingsWindowEventArgs(SettingsCategory category)
        {
            Category = category;
        }

        public SettingsCategory Category { get; }
    }
}
