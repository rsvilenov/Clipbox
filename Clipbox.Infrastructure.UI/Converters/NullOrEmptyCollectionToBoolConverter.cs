using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    public class NullOrEmptyCollectionToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            var collection = value as ICollection;
            bool result = collection != null && collection.Count != 0;
            Debug.WriteLine($"{nameof(NullOrEmptyCollectionToBoolConverter)}: {result}");
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
