using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Infrastructure.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Clipbox.Extentions.BuiltIn.TextFormats.FlowExtentions
{
    [ClipboxExtention]
    public class RemoveFormattingFlowExtention : FlowExtention
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            return items.Count() == 1 
                && IsParameterSet(FlowParameters.RemoveFormatting, true)
                && items.Single().ClipboardDataObjects.Any(cdo => cdo.ClipboardFormatName == "Rich Text Format");
        }

        public override IGeneratedClipboardItem OnPerformModifyRequested(IEnumerable<IClipboardItem> items)
        {
            var generated = new GeneratedClipboardItem();
            IClipboardDataObject textCdo = items.Single().ClipboardDataObjects.FirstOrDefault(cdo => cdo.ClipboardFormatName == "CF_UNICODETEXT" || cdo.ClipboardFormatName == "Text");

            if (textCdo != null)
            {
                generated.AddFormatAndData(textCdo.ClipboardFormatName, textCdo.OriginalData, ClipboardContentClass.Text);
            }
            else
            {
                IClipboardDataObject rtfCdo = items.Single().ClipboardDataObjects.FirstOrDefault(cdo => cdo.ClipboardFormatName == "Rich Text Format");
                string rtf = (string)rtfCdo.OriginalData;

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

                        generated.AddFormatAndData("CF_UNICODETEXT", plainText, ClipboardContentClass.Text);
                        generated.AddFormatAndData("Text", plainText, ClipboardContentClass.Text);
                    }));
                }
                
            }

            return generated;
        }
    }
}
