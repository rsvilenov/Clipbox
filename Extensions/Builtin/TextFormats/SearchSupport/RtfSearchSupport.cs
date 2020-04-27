using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{D8F8C369-7F81-4138-9718-4F34EB1BD669}")]
    public class RtfSearchSupport : SearchSupportExtension<RichTextIntermediateFormat>
    {
        protected override string GetSearchTextData(RichTextIntermediateFormat intermediateFormat)
        {
            string text = string.Empty;
            string rtf = intermediateFormat.Rtf;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        RichTextBox rtb = new RichTextBox
                        {
                            Background = System.Windows.Media.Brushes.Gainsboro
                        };

                        rtb.SelectAll();

                        var rtfBytes = Encoding.ASCII.GetBytes(rtf);
                        using (MemoryStream ms = new MemoryStream(rtfBytes))
                        {
                            rtb.Selection.Load(ms, DataFormats.Rtf);
                        }

                        text = rtb.Selection.Text;
                    }));
            }

            return text?.ReplaceNonAlphaWith(' ');
        }
    }
}
