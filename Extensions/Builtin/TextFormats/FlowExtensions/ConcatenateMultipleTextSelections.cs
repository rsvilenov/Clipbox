using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Infrastructure.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtensionAttribute]
    public class ConcatenateMultipleTextSelections : FlowExtension
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            return items.Count() > 1
                && items.All(item => item.ClipboardDataObjects
                    .Any(cdo => cdo.IntermediateFormatObject is RichTextIntermediateFormat
                    || cdo.IntermediateFormatObject is UnicodeTextIntermediateFormat 
                    || cdo.IntermediateFormatObject is TextIntermediateFormat));
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

                        IClipboardDataObject rtfCdo = item.ClipboardDataObjects.FirstOrDefault(cdo => cdo.IntermediateFormatObject is RichTextIntermediateFormat);
                        if (rtfCdo != null)
                        {
                            string rtf = ((RichTextIntermediateFormat)rtfCdo.IntermediateFormatObject).Rtf;
                            var rtfBytes = Encoding.UTF8.GetBytes(rtf);
                            using (MemoryStream ms = new MemoryStream(rtfBytes))
                            {
                                rtb.Selection.Load(ms, DataFormats.Rtf);
                            }
                            rtb.AppendText(separator);
                            continue;
                        }

                        IClipboardDataObject unicodeCdo = item.ClipboardDataObjects
                        .FirstOrDefault(cdo => cdo.IntermediateFormatObject is UnicodeTextIntermediateFormat);
                        if (unicodeCdo != null)
                        {
                            string unicodeText = ((UnicodeTextIntermediateFormat)unicodeCdo.IntermediateFormatObject).Text;
                            rtb.AppendText(unicodeText);
                            rtb.AppendText(separator);
                        }

                        IClipboardDataObject textCdo = item.ClipboardDataObjects
                        .FirstOrDefault(cdo => cdo.IntermediateFormatObject is TextIntermediateFormat);
                        if (textCdo != null)
                        {
                            string text = ((TextIntermediateFormat)textCdo.IntermediateFormatObject).Text;
                            rtb.AppendText(text);
                            rtb.AppendText(separator);
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
                        generated.AddFormatAndIntermediateFormatObject("Rich Text Format", new RichTextIntermediateFormat(rtfFromRtb));
                    }

                    generated.AddFormatAndIntermediateFormatObject("CF_UNICODETEXT", new UnicodeTextIntermediateFormat(textWithoutFormatting));
                    generated.AddFormatAndIntermediateFormatObject("Text", new TextIntermediateFormat(textWithoutFormatting));
                }));
            }
            
            return generated;
        }
    }
}
