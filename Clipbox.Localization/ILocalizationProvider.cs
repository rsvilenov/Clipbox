using Clipbox.Infrastructure.UI.Localization.EventArgsClasses;
using System;
using System.Globalization;

namespace Clipbox.Infrastructure.UI.Localization
{
    public interface ILocalizationProvider
    {
        CultureInfo CurrentCulture { get; set; }

        string GetTranslation(string key);

        event EventHandler<CultureChangedEventArgs> LocalizationCultureChanged;
    }
}
