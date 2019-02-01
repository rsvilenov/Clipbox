using System;
using System.Globalization;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    public class MathAddConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 0;
            foreach (var val in values)
            {
                double valDouble = 0;
                string valStr = val.ToString();
                if (!double.TryParse(valStr, out valDouble))
                {
                    throw new ArgumentException($"Got '{valStr}', which is not a number.");
                }

                result += valDouble;
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
