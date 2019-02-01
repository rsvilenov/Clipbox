using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class FormattedTextMeasuring
    {
        public static Size MeasureText(string text,
            FontFamily family, double size, FontStyle style, FontWeight weight, FontStretch stretch, TextFormattingMode formattingMode)
        {
            FormattedText formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, style, weight, stretch),
                size,
                Brushes.Black,
                null,
                formattingMode);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public static Size MeasureText(string text, TextBlock textBlock)
        {
            return MeasureText(text,
                        textBlock.FontFamily,
                        textBlock.FontSize,
                        textBlock.FontStyle,
                        textBlock.FontWeight,
                        textBlock.FontStretch,
                        TextOptions.GetTextFormattingMode(textBlock));
        }


        public static Size MeasureText(string text, Control control)
        {
            return MeasureText(text, control.FontFamily, control.FontSize,
                control.FontStyle, control.FontWeight, control.FontStretch,
                TextOptions.GetTextFormattingMode(control));
        }

    }
}
