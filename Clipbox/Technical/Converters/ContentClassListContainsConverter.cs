using Clipbox.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Clipbox.Technical.Converters
{
    public class ContentClassListContainsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = value as IEnumerable<ClipboardContentClass>;
            if (enumerable == null)
            {
                throw new ArgumentException("'value' should be IEnumerable<ClipboardContentClass>.");
            }

            return enumerable.Any(el => el.Equals(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
