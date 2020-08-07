using Clipbox.Infrastructure.Extensibility.Loading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Views.Settings.EventArgsClasses
{
    public class EnabledExtensionsEventArgs : EventArgs
    {
        public EnabledExtensionsEventArgs(IEnumerable<PersistableExtensionInfo> enabledExtensions)
        {
            EnabledExtensions = enabledExtensions?.ToList().AsReadOnly();
        }

        public IReadOnlyList<PersistableExtensionInfo> EnabledExtensions { get; set; }
    }
}
