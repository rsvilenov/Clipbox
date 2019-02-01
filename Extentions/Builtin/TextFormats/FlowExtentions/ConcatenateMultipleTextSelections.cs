using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.Enums;
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
    public class ConcatenateMultipleTextSelections : FlowExtention
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            return items.Count() > 1
                && items.All(item => item.ClipboardDataObjects.Any(cdo => cdo.Format == "Rich Text Format" || cdo.Format == "CF_UNICODETEXT" || cdo.Format == "Text"));
        }

        public override IGeneratedClipboardItem OnPerformModifyRequested(IEnumerable<IClipboardItem> items)
        {
            var generated = new GeneratedClipboardItem();

            string separator = string.Empty;
            if (IsParameterSet(FlowParameters.TextConcatenateSeparator, TextConcatenateSeparators.NewLine))
            {
                separator = Environment.NewLine;
            }
            else if (IsParameterSet(FlowParameters.TextConcatenateSeparator, TextConcatenateSeparators.Space))
            {
                separator = " ";
            }


            using (SimpleDispatcher dispatcher = new SimpleDispatcher())
            {
                // Ensure that we use the wpf control from within a STA thread
                // and that we wait for the thread's competion before we contunie
                dispatcher.Invoke(new Action(() =>
                {
                    RichTextBox rtb = new RichTextBox();
                    rtb.SelectAll();

                    foreach (IClipboardItem item in items)
                    {
                        // Change the selection each time, so that Selection.Load() does not overweite the previous contents
                        rtb.Selection.Select(rtb.Document.ContentEnd, rtb.Document.ContentEnd);

                        IClipboardDataObject rtfCdo = item.ClipboardDataObjects.FirstOrDefault(cdo => cdo.Format == "Rich Text Format");
                        if (rtfCdo != null)
                        {
                            string rtf = (string)rtfCdo.OriginalData;
                            var rtfBytes = Encoding.ASCII.GetBytes(rtf);
                            using (MemoryStream ms = new MemoryStream(rtfBytes))
                            {
                                rtb.Selection.Load(ms, DataFormats.Rtf);
                            }
                            rtb.AppendText(separator);
                            continue;
                        }

                        IClipboardDataObject unicodeCdo = item.ClipboardDataObjects.FirstOrDefault(cdo => cdo.Format == "CF_UNICODETEXT");
                        if (unicodeCdo != null)
                        {
                            string unicodeText = (string)unicodeCdo.OriginalData;
                            var unicodeBytes = Encoding.Unicode.GetBytes(unicodeText);
                            using (MemoryStream ms = new MemoryStream(unicodeBytes))
                            {
                                // we will set the dataformat to Rtf, because it seems that
                                // UnicodeText is not supported.
                                rtb.Selection.Load(ms, DataFormats.Rtf);
                            }
                            rtb.AppendText(separator);
                            continue;
                        }

                        IClipboardDataObject textCdo = item.ClipboardDataObjects.FirstOrDefault(cdo => cdo.Format == "Text");
                        if (textCdo != null)
                        {
                            string text = (string)textCdo.OriginalData;
                            var textBytes = Encoding.ASCII.GetBytes(text);
                            using (MemoryStream ms = new MemoryStream(textBytes))
                            {
                                rtb.Selection.Load(ms, DataFormats.Text);
                            }
                            rtb.AppendText(separator);
                            continue;
                        }

                    }


                    string textWithoutFormatting = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;

                    string rtfFromRtb = string.Empty;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        TextRange range2 = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        range2.Save(ms, DataFormats.Rtf);
                        ms.Seek(0, SeekOrigin.Begin);
                        using (StreamReader sr = new StreamReader(ms))
                        {
                            rtfFromRtb = sr.ReadToEnd();
                        }
                    }

                    if (!IsParameterSet(FlowParameters.RemoveFormatting, true))
                    {
                        generated.AddFormatAndData("Rich Text Format", rtfFromRtb);
                    }

                    generated.AddFormatAndData("CF_UNICODETEXT", textWithoutFormatting, ClipboardContentClass.Text);
                    generated.AddFormatAndData("Text", textWithoutFormatting, ClipboardContentClass.Text);
                }));
            }
            
            return generated;
        }
    }
}
