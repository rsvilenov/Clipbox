using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.BindableObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts.EventArgsClasses
{
    public class InstalledApplicationInfoEventArgs : EventArgs
        {
            public InstalledApplicationInfoEventArgs(string captionSearchString, IEnumerable<InstalledApplicationInfo> selectedApplications)
            {
                if (selectedApplications == null)
                {
                    throw new ArgumentNullException(nameof(selectedApplications));
                }

                SelectedApplications = selectedApplications.ToList().AsReadOnly();
            }

            public IReadOnlyList<InstalledApplicationInfo> SelectedApplications { get; set; }
        }
}
