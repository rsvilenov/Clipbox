using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Technical.ExtensionMethods;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Clipbox.Technical.Converters
{
    public class DateTimeToHumanReadableDateString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime == false)
            {
                Debug.WriteLine($"{nameof(value)} is not DateTime.");
                return null;
            }

            DateTime dt = (DateTime)value;

            string humanReadableDateString = dt
                .Date
                .ToWindowsStyleHumanReadableString();

            if (humanReadableDateString.StartsWith("LBL_"))
            {
                humanReadableDateString = TranslationSourcePool
                    .Instance
                    .GetSource()
                    .GetTranslation(humanReadableDateString);
            }

            return humanReadableDateString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
