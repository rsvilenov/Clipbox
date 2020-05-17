using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
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

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension]
    public class SkipShortStringsFlowExtension : FlowExtension
    {
        public override void OnItemStoring(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, ref IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            int? textLength = GetTextLength(originalIntermediateFormats);

            if (textLength.HasValue)
            {
                int minTextLength = GetSetting<int>(ExtensionSettingNames.MinTextLengthToStore);

                if (minTextLength > 0 /* 0 means infinite */
                    && textLength.Value < minTextLength)
                {
                    cancelStoreToItem = true;
                }
            }
        }

        private int? GetTextLength(IEnumerable<IIntermediateFormat> originalIntermediateFormats)
        {
            int? textLength = originalIntermediateFormats
                .Where(ifo => ifo is TextIntermediateFormat)
                .Select(ifo => ((TextIntermediateFormat)ifo).Text?.Length)
                .Union(
                    originalIntermediateFormats
                    .Where(ifo => ifo is UnicodeTextIntermediateFormat)
                    .Select(ifo => ((UnicodeTextIntermediateFormat)ifo).Text?.Length))
                .FirstOrDefault();

            if (!textLength.HasValue)
            {
                string rtf = originalIntermediateFormats
                    .Where(ifo => ifo is RichTextIntermediateFormat)
                    .Select(ifo => ((RichTextIntermediateFormat)ifo).Rtf)
                    .FirstOrDefault();

                if (rtf != null)
                {
                    // this is slow, but it is a rare case,
                    // as most applications put plain text 
                    // into the clipboard along wiht the rtf.
                    // So in most cases we should already
                    // have determined the text length by now.
                    textLength = GetTextLengthFromRtf(rtf);
                }
            }

            return textLength;
        }

        private int? GetTextLengthFromRtf(string rtf)
        {
            int? textLength = null;

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

                        textLength = wholeDoc.Text?.Length;
                    }));
            }

            return textLength;
        }
    }
}
