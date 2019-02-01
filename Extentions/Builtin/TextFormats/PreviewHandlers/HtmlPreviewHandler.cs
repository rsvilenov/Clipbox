using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewHandlers
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_HTML")]
    public class HtmlPreviewHandler : PreviewHandler<HtmlPreviewData>
    {
        public override HtmlPreviewData GetPreview(object data, int previewWidth, int previewHeight)
        {
            string html = data as string;
            if (html == null)
            {
                throw new ArgumentException("data is null or not a string");
            }

            Bitmap previewBmp = null;

            var waitScreenshotResetEvent = new ManualResetEvent(true);
            
            using (WebBrowser webBrowser = new WebBrowser())
            {
                webBrowser.NavigateToString(html);

                webBrowser.Navigated += (sender, args) =>
                {
                    // render the whole webbrowser control
                    RenderTargetBitmap rtBmp = new RenderTargetBitmap(previewWidth, previewHeight, 96, 96, PixelFormats.Pbgra32);
                    rtBmp.Render(webBrowser);

                    // RenderTargetBitmap is a bitmap source
                    previewBmp = rtBmp.ToBitmap();

                    waitScreenshotResetEvent.Reset();
                };

                waitScreenshotResetEvent.WaitOne();
            }

            return new HtmlPreviewData
            {
                Preview = previewBmp
            };
        }
    }
}
