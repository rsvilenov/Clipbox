using System;
using System.Globalization;

namespace Clipbox.Infrastructure.UI.Localization.EventArgsClasses
{
    public class CultureChangedEventArgs : EventArgs
    {
        public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
        {
            OldCulture = oldCulture;
            NewCulture = newCulture;
        }

        public CultureInfo OldCulture { get; }
        public CultureInfo NewCulture { get; }
    }
}
