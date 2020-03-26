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

            DateTime dt = (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds);

            return dt.ToWindowsStyleHumanReadableString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
