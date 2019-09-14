using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Clipbox.Infrastructure.UI.Converters
{
    /// <summary>
    /// Convreter that converts false to Hidden, instead of Collapsed. 
    /// Modeled after WPF's built-in BooleanToVisibilityConverter, which converts false to Collapsed.
    /// <see href="https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/Controls/BooleanToVisibilityConverter.cs.html"/>>
    /// </summary>
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class BooleanToVisibilityHiddenConverter : IValueConverter
    {
        /// <summary>
        /// Convert bool or Nullable&lt;bool&gt; to Visibility
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = false;
            if (value is bool)
            {
                bValue = (bool)value;
            }
            else if (value is Nullable<bool>)
            {
                Nullable<bool> tmp = (Nullable<bool>)value;
                bValue = tmp ?? false;
            }
            return (bValue) ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Convert Visibility to boolean
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (Visibility)value == Visibility.Visible;
            }
            else
            {
                return false;
            }
        }
    }
}
