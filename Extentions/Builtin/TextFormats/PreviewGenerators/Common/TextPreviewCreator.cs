using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewGenerators.Common
{
    public static class TextPreviewCreator
    {
        public enum TextFormat
        {
            Text,
            Rtf
        }

        public static ImagePreviewData GeneratePreview(string textData, TextFormat format, int desiredPreviewWidth, int maxPreviewHeight)
        {
            RichTextBox rtb = new RichTextBox
            {
                Background = System.Windows.Media.Brushes.Gainsboro
            };

            if (format == TextFormat.Text)
            {
                // TODO: consider changing this according to the dpi, or perhaps scaling the entire RichTextBox
                rtb.FontSize = 14;
            }

            rtb.SelectAll();

            var textDataBytes = Encoding.UTF8.GetBytes(textData);
            using (MemoryStream ms = new MemoryStream(textDataBytes))
            {
                rtb.Selection.Load(ms, format == TextFormat.Text ? DataFormats.Text : DataFormats.Rtf);
            }

            string text = rtb.Selection.Text;
            int textLength = text.Length;
            int wordCount = text.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            double maxRtbWidth = desiredPreviewWidth * 3;
            double maxRtbHeight = desiredPreviewWidth * 3;

            rtb.Measure(new System.Windows.Size(maxRtbWidth, maxRtbHeight));
            rtb.Arrange(new Rect(rtb.DesiredSize));

            rtb.UpdateLayout();

            RtbTextProcessor rtbProc = new RtbTextProcessor();
            rtbProc.AdjustSpacing(rtb);
            rtbProc.TrimSeparatorCharSeries(rtb, RtbTextProcessor.SeparatorChars.Enter);
            rtbProc.TrimSeparatorCharSeries(rtb, RtbTextProcessor.SeparatorChars.Space);
            rtbProc.TrimSeparatorCharSeries(rtb, RtbTextProcessor.SeparatorChars.Tab);
            
            FormattedText ft = FlowDocumentToFormattedTextConverter.GetFormattedText(rtb.Document);

            double newRtbWidth = ft.WidthIncludingTrailingWhitespace;
            if (newRtbWidth > maxRtbWidth)
            {
                newRtbWidth = maxRtbWidth;
            }
            else if (newRtbWidth < desiredPreviewWidth)
            {
                newRtbWidth = desiredPreviewWidth;
            }

            // hack: after the measuring phase, it seems that the desired hight is less than the text height, hence maxRtbHeight + 20 
            rtb.Measure(new System.Windows.Size(newRtbWidth, maxRtbHeight+20));
            rtb.Arrange(new Rect(rtb.DesiredSize));
            rtb.UpdateLayout();

            RenderTargetBitmap rtBmp = RenderVisual(rtb);

            Bitmap preview = null;
            using (Bitmap previewBmp = rtBmp.ToBitmap())
            {
                if (previewBmp.Height == maxPreviewHeight)
                {
                    preview = previewBmp.DeepClone();
                }
                else if (previewBmp.Height > maxPreviewHeight)
                {
                    preview = previewBmp.Crop(new Rectangle
                    {
                        X = 0,
                        Y = 0,
                        Width = previewBmp.Width,
                        Height = maxPreviewHeight
                    });
                }
                else if (previewBmp.Height < maxPreviewHeight)
                {
                    preview = previewBmp.GetCenteredOverTransparentCanvas(previewBmp.Width, maxPreviewHeight);
                }
                else
                {
                    throw new InvalidOperationException("The width of the image should have been ensured to be >= desiredPreviewWidth by now.");
                }
            }
            return new ImagePreviewData
            {
                Preview = preview,
                Annotation = $"{wordCount}/{textLength}"
            };
        }

        /// <summary>
        /// https://stackoverflow.com/questions/14118003/snapshot-of-an-wpf-canvas-area-using-rendertargetbitmap
        /// ILIA BROUDNO's answer
        /// </summary>
        /// <param name="elt"></param>
        /// <returns></returns>
        private static RenderTargetBitmap RenderVisual(UIElement elt)
        {
            PresentationSource source = PresentationSource.FromVisual(elt);
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)elt.RenderSize.Width,
                  (int)elt.RenderSize.Height, 96, 96, PixelFormats.Default);

            VisualBrush sourceBrush = new VisualBrush(elt);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            using (drawingContext)
            {
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0),
                      new System.Windows.Point(elt.RenderSize.Width, elt.RenderSize.Height)));
            }
            rtb.Render(drawingVisual);

            return rtb;
        }


    }
}
