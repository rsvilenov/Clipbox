using System;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    public class ComparisonConverter : IValueConverter
    {
        // a very convenient soltion from Scott's answer in this thread:
        // https://stackoverflow.com/questions/397556/how-to-bind-radiobuttons-to-an-enum
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
