using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.ColorPicker
{
    public class GetContrastingSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color passedColor = Colors.White;


            if (value is SolidColorBrush brush)
            {
                passedColor = brush.Color;
            }
            else if (value is string hexValue
                && !string.IsNullOrWhiteSpace(hexValue)
                && hexValue.StartsWith("#"))
            {
                try
                {
                    passedColor = (Color)ColorConverter.ConvertFromString(hexValue);
                }
                catch { }
            }
            
            return new SolidColorBrush(ColorPicker.GetContrastColor(passedColor));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
