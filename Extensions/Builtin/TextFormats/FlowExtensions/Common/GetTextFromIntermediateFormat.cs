using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions.Common
{
    internal class GetTextFromIntermediateFormat
    {
        public static string GetText(IEnumerable<IIntermediateFormat> intermediateFormats)
        {
            string text = intermediateFormats
                .Where(ifo => ifo is TextIntermediateFormat)
                .Select(ifo => ((TextIntermediateFormat)ifo).Text)
                .FirstOrDefault();

            if (text == null)
            {
                text = intermediateFormats
                    .Where(ifo => ifo is UnicodeTextIntermediateFormat)
                    .Select(ifo => ((UnicodeTextIntermediateFormat)ifo).Text)
                    .FirstOrDefault();
            }

            if (text == null)
            {
                string rtf = intermediateFormats
                    .Where(ifo => ifo is RichTextIntermediateFormat)
                    .Select(ifo => ((RichTextIntermediateFormat)ifo).Rtf)
                    .FirstOrDefault();

                if (rtf != null)
                {
                    // this is slow, but it is a rare case,
                    // as most applications put plain text 
                    // into the clipboard along wiht the rtf.
                    // So in most cases we should already
                    // have got the text by now.
                    text = GetTextFromRtf(rtf);
                }
            }

            return text;
        }

        private static string GetTextFromRtf(string rtf)
        {
            string text = null;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        RichTextBox rtb = new RichTextBox();
                        var rtfBytes = Encoding.UTF8.GetBytes(rtf);

                        TextRange wholeDoc = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        using (MemoryStream ms = new MemoryStream(rtfBytes))
                        {
                            wholeDoc.Load(ms, DataFormats.Rtf);
                        }

                        text = wholeDoc.Text;
                    }));
            }

            return text;
        }
    }
}
