using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Threading;
using System.Windows.Controls;

namespace Clipbox.Extentions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_HTML")]
    public class HtmlSearchSupport : SearchSupportExtension
    {
        public override string GetSearchTextData(object data)
        {
            if (!(data is string html))
            {
                throw new ArgumentException("data is null or not a string");
            }

            string text = string.Empty;

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

                            if (webBrowser.Document is MSHTML.HTMLDocument doc)
                            {
                                text = doc.body?.innerText ?? string.Empty;
                            }
                        }
                    }));

                return text;
            }
        }
    }
}
