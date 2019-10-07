using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewHandlers
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("Rich Text Format")]
    public class RtfPreviewHandler : PreviewHandler<ImagePreviewData>
    {
        public override ImagePreviewData GetPreview(object data, int previewWidth, int previewHeight)
        {
            string rtf = data as string;
            if (rtf == null)
            {
                throw new ArgumentException("data is null or not a string");
            }
            
            Bitmap preview = null;
            int textLength = 0;
            int wordCount = 0;
            
            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        RichTextBox rtb = new RichTextBox();
                        rtb.Background = System.Windows.Media.Brushes.Gainsboro;
                        rtb.SelectAll();

                        var rtfBytes = Encoding.UTF8.GetBytes(rtf);
                        using (MemoryStream ms = new MemoryStream(rtfBytes))
                        {
                            rtb.Selection.Load(ms, DataFormats.Rtf);
                        }

                        textLength = rtb.Selection.Text.Length;
                        wordCount = rtb.Selection.Text.Split(new char[] { ' ' },
                            StringSplitOptions.RemoveEmptyEntries).Length;

                        rtb.HorizontalAlignment = HorizontalAlignment.Center;
                        rtb.VerticalAlignment = VerticalAlignment.Center;


                        //FormattedText fmtText = GetFormattedText(rtb.Document);
                        //if (fmtText.Height > previewHeight)
                        //{

                        //}

                        rtb.Measure(new System.Windows.Size(previewWidth, previewHeight));
                        rtb.Arrange(new Rect(rtb.DesiredSize));

                        rtb.UpdateLayout();

                        RenderTargetBitmap rtBmp = new RenderTargetBitmap((int)rtb.ActualWidth, (int)rtb.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        rtBmp.Render(rtb);

                        //DrawingVisual dv = new DrawingVisual();
                        //Rect bounds = new Rect(new System.Windows.Point(), rtb.DesiredSize); //VisualTreeHelper.GetDescendantBounds(rtb);


                        //using (DrawingContext ctx = dv.RenderOpen())
                        //{
                        //    VisualBrush vb = new VisualBrush(rtb);
                        //    ctx.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), bounds.Size));
                        //}
                        //rtBmp.Render(dv);

                        using (Bitmap previewBmp = rtBmp.ToBitmap())
                        {
                            //.GetResized(previewWidth, previewHeight);

                            //System.Windows.Size textSize = FormattedTextMeasuring.MeasureText(rtb.Selection.Text, rtb);

                            //if (previewHeight > textSize.Height && previewWidth > textSize.Width)
                            //{
                            //    previewBmp = previewBmp.GetResized((int)((double)previewBmp.Width * 1.3), (int)((double)previewBmp.Height * 1.3));
                            //}

                            if (previewBmp.Width == previewWidth && previewBmp.Height == previewHeight)
                            {
                                preview = previewBmp.DeepClone();
                            }
                            else if (previewBmp.Width > previewWidth || previewBmp.Height > previewHeight)
                            {
                                throw new InvalidOperationException("The RichTextBox should have been resized to the thumbnail size already.");
                            }
                            else // if the preview is smaller than the preview size, superimpose it at the center of a transparent canvas
                            {
                                preview = previewBmp.GetCenteredOverTransparentCanvas(previewWidth, previewHeight);
                            }
                        }
                    }));
            }

            return new ImagePreviewData
            {
                Preview = preview,
                Annotation = $"{wordCount}/{textLength}"
            };

        }

        //// The methods below are taken from Min Zhu's answer in this thread:
        //// https://social.msdn.microsoft.com/Forums/vstudio/en-US/6cd49173-b06d-4749-85aa-f6ab46c7d4af/wpf-rich-text-box-width-size-adjust-to-text?forum=wpf


        //private IEnumerable<TextElement> GetRunsAndParagraphs(FlowDocument doc)
        //{
        //    for (TextPointer position = doc.ContentStart;
        //      position != null && position.CompareTo(doc.ContentEnd) <= 0;
        //      position = position.GetNextContextPosition(LogicalDirection.Forward))
        //    {
        //        if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
        //        {
        //            Run run = position.Parent as Run;

        //            if (run != null)
        //            {
        //                yield return run;
        //            }
        //            else
        //            {
        //                Paragraph para = position.Parent as Paragraph;

        //                if (para != null)
        //                {
        //                    yield return para;
        //                }
        //            }
        //        }
        //    }
        //}

        //private FormattedText GetFormattedText(FlowDocument doc)
        //{
        //    if (doc == null)
        //    {
        //        throw new ArgumentNullException("doc");
        //    }

        //    FormattedText output = new FormattedText(
        //      GetText(doc),
        //      CultureInfo.CurrentCulture,
        //      doc.FlowDirection,
        //      new Typeface(doc.FontFamily, doc.FontStyle, doc.FontWeight, doc.FontStretch),
        //      doc.FontSize,
        //      doc.Foreground);

        //    int offset = 0;

        //    foreach (TextElement el in GetRunsAndParagraphs(doc))
        //    {
        //        Run run = el as Run;

        //        if (run != null)
        //        {
        //            int count = run.Text.Length;

        //            output.SetFontFamily(run.FontFamily, offset, count);
        //            output.SetFontStyle(run.FontStyle, offset, count);
        //            output.SetFontWeight(run.FontWeight, offset, count);
        //            output.SetFontSize(run.FontSize, offset, count);
        //            output.SetForegroundBrush(run.Foreground, offset, count);
        //            output.SetFontStretch(run.FontStretch, offset, count);
        //            output.SetTextDecorations(run.TextDecorations, offset, count);

        //            offset += count;
        //        }
        //        else
        //        {
        //            offset += Environment.NewLine.Length;
        //        }
        //    }

        //    return output;
        //}

        //private string GetText(FlowDocument doc)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    foreach (TextElement el in GetRunsAndParagraphs(doc))
        //    {
        //        Run run = el as Run;
        //        sb.Append(run == null ? Environment.NewLine : run.Text);
        //    }
        //    return sb.ToString();
        //}




    }
}
