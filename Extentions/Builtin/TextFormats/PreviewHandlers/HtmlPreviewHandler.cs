using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI.Utils;
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

            using (OwnThreadDispatcher dispatcher = new OwnThreadDispatcher())
            {
                dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        using (var waitScreenshotResetEvent = new AutoResetEvent(false))
                        using (WebBrowser webBrowser = new WebBrowser())
                        {
                            webBrowser.NavigateToString(html);

                            webBrowser.Navigated += (sender, args) =>
                            {
                                waitScreenshotResetEvent.Set();
                            };

                            waitScreenshotResetEvent.WaitOne();

                            // without this, according to one source, a memory leak may occur during rendering
                            webBrowser.UpdateLayout();

                            // render the whole webbrowser control
                            RenderTargetBitmap rtBmp = new RenderTargetBitmap(previewWidth, previewHeight, 96, 96, PixelFormats.Pbgra32);
                            rtBmp.Render(webBrowser);

                            // RenderTargetBitmap is a bitmap source
                            previewBmp = rtBmp.ToBitmap();
                        }
                    }));
            }
            

            return new HtmlPreviewData
            {
                Preview = previewBmp
            };
        }
    }
}
