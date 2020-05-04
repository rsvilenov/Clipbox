using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators.Common
{
    public class TextFormatsPreviewCreator
    {
        public enum TextFormat
        {
            Text,
            Rtf
        }

        public class PreviewGenerationOptions
        {
            public bool InclideAnnotation { get; set; } = true;

            public bool PreserveRtfFormatting { get; set; } = true;

            public System.Windows.Media.Color? Background { get; set; } =  null;

            public System.Windows.Media.Color? Foreground { get; set; } = null;

            public System.Windows.Media.Color? FormattingCharsForeground { get; set; } = null;

            public double FontScaleFactor { get; set; } = 14;

            public bool MakeTextCompact { get; set; } = true;

            public void Validate(TextFormat format)
            {
                if (format == TextFormat.Rtf
                    && PreserveRtfFormatting
                    && Foreground.HasValue)
                {
                    throw new InvalidOperationException($"When a preview for an rtf with preserved formatting is requested, the foreground cannot be changed.");
                }
            }
        }
        
        public static ImagePreviewData GeneratePreview(string textData,
            TextFormat format,
            int desiredPreviewWidth,
            int maxPreviewHeight,
            double dpi,
            bool areDimensionsScaledToDpi,
            PreviewGenerationOptions options)
        {
            RichTextBox rtb = new RichTextBox
            {
                Background = new SolidColorBrush(options.Background ?? System.Windows.Media.Colors.Gainsboro),
            };

            if (options.Foreground.HasValue)
            {
                rtb.Foreground = new SolidColorBrush(options.Foreground.Value);
            }

            rtb.SelectAll();

            try
            {
                RtfProcessor rp = new RtfProcessor(options.FormattingCharsForeground ?? Colors.Brown, 12 * 2);
                if (format == TextFormat.Text)
                {
                    if (!rp.TryLoadFromText(textData, 12 * 2))
                    {
                        rtb.FontSize = 12;
                        string rtfFromRtb = GetRtfUsingRichTextBox(textData, rtb);
                        rp.LoadFromRtf(rtfFromRtb);
                    }
                }
                else
                {
                    rp.LoadFromRtf(textData);
                }

                rp.ScaleFontSize(options.FontScaleFactor);

                string resultingText = null;
                if (options.MakeTextCompact)
                {
                    rp.ReplaceSpacingChar(RtfProcessor.SpacingChars.LineBreak);
                    rp.ReplaceSpacingChar(RtfProcessor.SpacingChars.Tab);
                    rp.ReplaceSpacingChar(RtfProcessor.SpacingChars.Space);
                }
                
                resultingText = rp.GetProcessedRtf();

                LoadTextDataIntoRichTextBox(resultingText ?? textData, TextFormat.Rtf, rtb);
            }
            catch
            {
                // load the text directly
                LoadTextDataIntoRichTextBox(textData, format, rtb);
            }

            string text = rtb.Selection.Text;
            if (!options.PreserveRtfFormatting)
            {
                LoadTextDataIntoRichTextBox(text, TextFormat.Text, rtb);
            }

            int textLength = text.Length;
            int wordCount = text.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            int lineCount = text.Split(new char[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            double maxRtbWidth = desiredPreviewWidth * 3;
            double maxRtbHeight = desiredPreviewWidth * 3;

            if (format == TextFormat.Text)
            {
                AdjustLineSpacing(rtb);
            }

            rtb.Measure(new System.Windows.Size(maxRtbWidth, maxRtbHeight));
            rtb.Arrange(new Rect(rtb.DesiredSize));

            rtb.UpdateLayout();
            
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
            rtb.Measure(new System.Windows.Size(newRtbWidth, maxRtbHeight + 20));
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


            string annotation = string.Empty;
            if (options.InclideAnnotation)
            {
                var resManager = Resources.Translations.Translations.ResourceManager;
                if (wordCount > 5 && wordCount < 15)
                {
                    annotation = $"{wordCount} {resManager.GetString("STR_WORDS", Thread.CurrentThread.CurrentUICulture)}";
                }
                else if (wordCount > 15)
                {
                    annotation = $"{lineCount} {resManager.GetString("STR_LINES", Thread.CurrentThread.CurrentUICulture)}";
                }
                else if (textLength > 1 || textLength == 0)
                {
                    annotation = $"{textLength} {resManager.GetString("STR_CHARACTERS", Thread.CurrentThread.CurrentUICulture)}";
                }
                else
                {
                    annotation = $"{textLength} {resManager.GetString("STR_CHARACTER", Thread.CurrentThread.CurrentUICulture)}";
                }
            }

            return new ImagePreviewData
            {
                Preview = preview,
                //Annotation = $"{wordCount}/{textLength}"
                Annotation = annotation
            };
        }

        private static void AdjustLineSpacing(RichTextBox rtb)
        {
            foreach (Paragraph p in rtb.Document.Blocks.OfType<Paragraph>())
            {
                p.Margin = new Thickness(0);
                p.LineHeight = 2;
            }
        }

        private static void LoadTextDataIntoRichTextBox(string textData, TextFormat format, RichTextBox rtb)
        {
            var textDataBytes = Encoding.UTF8.GetBytes(textData);

            TextRange wholeDoc = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            using (MemoryStream ms = new MemoryStream(textDataBytes))
            {
                wholeDoc.Load(ms, format == TextFormat.Text ? DataFormats.Text : DataFormats.Rtf);
            }
        }

        private static string GetRtfUsingRichTextBox(string textData, RichTextBox rtb)
        {
            var textDataBytes2 = Encoding.UTF8.GetBytes(textData);
            using (MemoryStream ms = new MemoryStream(textDataBytes2))
            {
                rtb.Selection.Load(ms, DataFormats.Text);
            }

            string rtfFromRtb = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                TextRange range2 = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                range2.Save(ms, DataFormats.Rtf);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    rtfFromRtb = sr.ReadToEnd();
                    return rtfFromRtb;
                }
            }
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
