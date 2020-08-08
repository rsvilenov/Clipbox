using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions
{
    [ClipboxExtension(
    description: "Masks/hides credit card numbers from the copied text",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    public class ProcessCCNumbersFlowExtension : FlowExtension
    {
        public override void OnItemStoring(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, ref IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            if (GetSetting<bool>(ExtensionSettingNames.SkipMaskCreditCardNumbers))
            {
                foreach (IIntermediateFormat ifo in originalIntermediateFormats)
                {
                    if (ifo is TextIntermediateFormat tifo)
                    {
                        string processedText = ProcessText(tifo.Text);
                        if (string.IsNullOrEmpty(processedText))
                        {
                            cancelStoreToItem = true;
                            break;
                        }
                        else
                        {
                            modifiedIntermediateFormats.Add(new TextIntermediateFormat(processedText));
                        }
                    }

                    else if (ifo is UnicodeTextIntermediateFormat utifo)
                    {
                        string processedText = ProcessText(utifo.Text);
                        if (string.IsNullOrEmpty(processedText))
                        {
                            cancelStoreToItem = true;
                            break;
                        }
                        else
                        {
                            modifiedIntermediateFormats.Add(new UnicodeTextIntermediateFormat(processedText));
                        }
                    }

                    else if (ifo is RichTextIntermediateFormat rtifo)
                    {
                        string processedRtf = ProcessText(rtifo.Rtf);
                        if (string.IsNullOrEmpty(processedRtf))
                        {
                            cancelStoreToItem = true;
                            break;
                        }
                        else
                        {
                            modifiedIntermediateFormats.Add(new RichTextIntermediateFormat(processedRtf));
                        }
                    }
                }
            }
        }

        private string ProcessText(string text)
        {
            const string CREDITCARD_REGEX = @"\b(?:\d[ -]*?){13,16}\b";
            const int MAX_TEXT_LEN_TO_REGEX_PROCESS = 5000;
            const int MAX_TEXT_LEN_TO_TRIM = 150;

            string processedText = string.Empty;

            if (text.Length < MAX_TEXT_LEN_TO_REGEX_PROCESS)
            {
                if (text.Length < MAX_TEXT_LEN_TO_TRIM)
                {
                    text = text.Trim();
                }

                Regex creditCardRegex = new Regex(CREDITCARD_REGEX);

                processedText = creditCardRegex.Replace(text, m =>
                {
                    if (m.Value == text)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return new string(m.Value.Select(c => char.IsDigit(c) ? 'X' : c).ToArray());
                    }
                });
            }

            return processedText;
        }
    }
}
