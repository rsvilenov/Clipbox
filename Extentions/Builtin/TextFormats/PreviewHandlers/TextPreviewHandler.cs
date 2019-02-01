using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.Threading;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewHandlers
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("Text")]
    [ClipboardFormat("CF_UNICODETEXT")]
    public class TextPreviewHandler : PreviewHandler<ImagePreviewData>
    {
        public override ImagePreviewData GetPreview(object data, int previewWidth, int previewHeight)
        {
            string text = data as string;
            if (text == null)
            {
                throw new ArgumentException("data is null or not a string");
            }

            Bitmap preview = null;
            int textLength = 0;
            int wordCount = 0;

            using (SimpleDispatcher dispatcher = new SimpleDispatcher())
            {
                // Ensure that we use the wpf control from within a STA thread
                // and that we wait for the thread's competion before we contunie
                dispatcher.Invoke(new Action(() =>
                {
                    RichTextBox rtb = new RichTextBox();
                    rtb.Background = System.Windows.Media.Brushes.Gainsboro;
                    rtb.SelectAll();

                    var textBytes = Encoding.ASCII.GetBytes(text);
                    using (MemoryStream ms = new MemoryStream(textBytes))
                    {
                        rtb.Selection.Load(ms, DataFormats.Text);
                    }

                    textLength = rtb.Selection.Text.Length;
                    wordCount = rtb.Selection.Text.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries).Length;

                    rtb.HorizontalAlignment = HorizontalAlignment.Center;
                    rtb.VerticalAlignment = VerticalAlignment.Center;

                    rtb.Measure(new System.Windows.Size(previewWidth, previewHeight));
                    rtb.Arrange(new Rect(rtb.DesiredSize));

                    RenderTargetBitmap rtBmp = new RenderTargetBitmap((int)rtb.ActualWidth, (int)rtb.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtBmp.Render(rtb);


                    using (Bitmap previewBmp = rtBmp.ToBitmap())
                    {
                        if (previewBmp.Width == previewWidth && previewBmp.Height == previewHeight)
                        {
                            preview = (Bitmap)previewBmp.Clone();
                        }
                        else if (previewBmp.Width > previewWidth || previewBmp.Height > previewHeight)
                        {
                            throw new InvalidOperationException("The TextBox should have been resized to the thumbnail size already.");
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

    }
}
