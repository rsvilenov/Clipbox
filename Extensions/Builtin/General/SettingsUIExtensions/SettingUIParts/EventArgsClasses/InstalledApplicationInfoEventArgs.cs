using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.BindableObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.EventArgsClasses
{
    public class InstalledApplicationInfoEventArgs : EventArgs
        {
            public InstalledApplicationInfoEventArgs(IEnumerable<InstalledApplicationInfo> selectedApplications)
            {
                SelectedApplications = selectedApplications?.ToList().AsReadOnly();
            }

            public IReadOnlyList<InstalledApplicationInfo> SelectedApplications { get; set; }
        }
}
