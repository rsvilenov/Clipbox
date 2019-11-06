using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewGenerators.Common
{
    /// <summary>
    /// This code is not mine. It comes from this blog:
    /// https://www.cnblogs.com/xunyiHe/p/10538959.html
    /// These methods appear to be the same as those in Min Zhu's answer in this thread:
    /// https://social.msdn.microsoft.com/Forums/vstudio/en-US/6cd49173-b06d-4749-85aa-f6ab46c7d4af/wpf-rich-text-box-width-size-adjust-to-text?forum=wpf
    /// </summary>
    public static class FlowDocumentToFormattedTextConverter
    {
        private static IEnumerable<TextElement> GetRunsAndParagraphs(FlowDocument doc)
        {
            for (TextPointer position = doc.ContentStart;
              position != null && position.CompareTo(doc.ContentEnd) <= 0;
              position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    Run run = position.Parent as Run;

                    if (run != null)
                    {
                        yield return run;
                    }
                    else
                    {
                        Paragraph para = position.Parent as Paragraph;

                        if (para != null)
                        {
                            yield return para;
                        }
                    }
                }
            }
        }

        private static string GetText(FlowDocument doc)
        {
            StringBuilder sb = new StringBuilder();

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                Run run = el as Run;
                sb.Append(run == null ? Environment.NewLine : run.Text);
            }
            return sb.ToString();
        }

        public static FormattedText GetFormattedText(FlowDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            FormattedText output = new FormattedText(
              GetText(doc),
              CultureInfo.InvariantCulture,
              doc.FlowDirection,
              new Typeface(doc.FontFamily, doc.FontStyle, doc.FontWeight, doc.FontStretch),
              doc.FontSize,
              doc.Foreground);

            int offset = 0;

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                Run run = el as Run;

                if (run != null)
                {
                    int count = run.Text.Length;

                    output.SetFontFamily(run.FontFamily, offset, count);
                    output.SetFontStyle(run.FontStyle, offset, count);
                    output.SetFontWeight(run.FontWeight, offset, count);
                    output.SetFontSize(run.FontSize, offset, count);
                    output.SetForegroundBrush(run.Foreground, offset, count);
                    output.SetFontStretch(run.FontStretch, offset, count);
                    output.SetTextDecorations(run.TextDecorations, offset, count);

                    offset += count;
                }
                else
                {
                    offset += Environment.NewLine.Length;
                }
            }

            return output;
        }
    }
}
