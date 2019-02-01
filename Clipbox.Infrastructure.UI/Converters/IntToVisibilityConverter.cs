using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        private int val;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return Visibility.Collapsed;

            int intParam = (int)parameter;
            val = (int)value;

            return ((intParam & val) != 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
