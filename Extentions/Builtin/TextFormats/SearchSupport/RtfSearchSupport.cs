using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clipbox.Extentions.BuiltIn.TextFormats.SearchSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("Rich Text Format")]
    public class RtfSearchSupport : SearchSupportExtension
    {
        public override string GetSearchTextData(object data)
        {
            if (!(data is string rtf))
            {
                throw new ArgumentException("data is null or not a string");
            }

            string text = string.Empty;

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

            return text;
        }
    }
}
