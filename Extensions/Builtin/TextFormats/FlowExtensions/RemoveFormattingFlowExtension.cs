using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension(
    description: "Removes the formatting from formatted text",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    public class RemoveFormattingFlowExtension : FlowExtension
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            return items.Count() == 1 
                && IsParameterSet(FlowParameters.RemoveFormatting, true)
                && items.Single().ClipboardDataObjects.
                    Any(cdo => cdo.IntermediateFormatMetadata.FormatGuid == "{D8F8C369-7F81-4138-9718-4F34EB1BD669}"); // RichTextIntermediateFormat
        }

        public override IGeneratedClipboardItem OnPerformModifyRequested(IEnumerable<IClipboardItem> items)
        {
            var generated = new GeneratedClipboardItem();
            IClipboardDataObject textCdo = items.Single().ClipboardDataObjects.
                FirstOrDefault(cdo => cdo.IntermediateFormatObject is UnicodeTextIntermediateFormat 
                    || cdo.IntermediateFormatObject is TextIntermediateFormat);

            if (textCdo != null)
            {
                generated.AddFormatAndIntermediateFormatObject(textCdo.ClipboardFormatName, textCdo.IntermediateFormatObject);
            }
            else
            {
                IClipboardDataObject rtfCdo = items.Single().ClipboardDataObjects.FirstOrDefault(cdo => cdo.IntermediateFormatObject is RichTextIntermediateFormat);
                string rtf = ((RichTextIntermediateFormat)rtfCdo.IntermediateFormatObject).Rtf;

                using (SimpleDispatcher dispatcher = new SimpleDispatcher())
                {
                    // Ensure that we use the wpf control from within a STA thread
                    // and that we wait for the thread's competion before we contunie
                    dispatcher.Invoke(new Action(() =>
                    {
                        RichTextBox rtb = new RichTextBox();
                        rtb.SelectAll();

                        var rtfBytes = Encoding.ASCII.GetBytes(rtf);
                        using (MemoryStream ms = new MemoryStream(rtfBytes))
                        {
                            rtb.Selection.Load(ms, DataFormats.Rtf);
                        }
                        string plainText = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;

                        generated.AddFormatAndIntermediateFormatObject("CF_UNICODETEXT", new UnicodeTextIntermediateFormat(plainText));
                        generated.AddFormatAndIntermediateFormatObject("Text", new TextIntermediateFormat(plainText));
                    }));
                }
                
            }

            return generated;
        }
    }
}
