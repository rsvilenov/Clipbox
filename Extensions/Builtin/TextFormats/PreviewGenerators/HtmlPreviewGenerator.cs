using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{5E4E9C1F-A324-42A4-AE9E-AA64DD871CC8}")]
    public class HtmlPreviewGenerator : SinglePreviewGenerator<HtmlIntermediateFormat, HtmlPreviewData>
    {
        protected override HtmlPreviewData GetPreview(HtmlIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            Bitmap previewBmp = null;
            string html = intermediateFormat.Html;

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
