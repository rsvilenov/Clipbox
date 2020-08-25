using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Clipbox.Technical.Converters
{
    public class TextFromTextBlockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TextBlock tb)
            {
                Drawing textBlockDrawing = VisualTreeHelper.GetDrawing(tb);
                if (textBlockDrawing == null)
                {
                    Debug.WriteLine("The TextBlock has not been rendered yet");
                    return string.Empty;
                }

                var sb = new StringBuilder();
                WalkDrawingForText(sb, textBlockDrawing);
                return sb.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // https://stackoverflow.com/questions/4319031/get-displayed-text-from-textblock
        private static void WalkDrawingForText(StringBuilder sb, Drawing d)
        {
            if (d is GlyphRunDrawing glyphs)
            {
                sb.Append(glyphs.GlyphRun.Characters.ToArray());
            }
            else
            {
                if (d is DrawingGroup g)
                {
                    foreach (Drawing child in g.Children)
                    {
                        WalkDrawingForText(sb, child);
                    }
                }
            }
        }
    }
}
