using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    public abstract class MathConverterBase : IMultiValueConverter
    {
        protected abstract double DoMathOperation(double number1, double number2);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 0;
            foreach (var val in values)
            {
                if (val == DependencyProperty.UnsetValue)
                {
                    // we have most probably been called from within a template,
                    // which does not have a DataSource at the time of Loading,
                    // and this is when our converter has been first called.
                    // Upon the second call, the value should be present.
                    return 0;
                }

                double valDouble = 0;
                string valStr = val.ToString();
                if (!double.TryParse(valStr, out valDouble))
                {
                    throw new ArgumentException($"Got '{valStr}', which is not a number.");
                }

                result = DoMathOperation(result, valDouble);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
