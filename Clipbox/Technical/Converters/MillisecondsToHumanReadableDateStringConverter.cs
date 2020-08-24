using Clipbox.Infrastructure.UI.Localization;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using static Clipbox.Technical.ExtensionMethods.DateTimeExtensions;

namespace Clipbox.Technical.Converters
{
    public class MillisecondsToHumanReadableDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double == false)
            {
                Debug.WriteLine($"{nameof(value)} is not double.");
                return null;
            }

            double milliseconds = (double)value;

            DateTime utcDt = (new DateTime(1970, 1, 1))
                .AddMilliseconds(milliseconds);

            string humanReadableDateString = utcDt
                .ToLocalTime()
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
