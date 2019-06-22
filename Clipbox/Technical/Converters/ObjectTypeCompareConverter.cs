using System;
using System.Globalization;
using System.Windows.Data;

namespace Clipbox.Technical.Converters
{
    public class ObjectTypeCompareConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is Type == false)
            {
                throw new ArgumentException($"{nameof(parameter)} should be of type System.Type");
            }

            return value.GetType() == (Type)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
