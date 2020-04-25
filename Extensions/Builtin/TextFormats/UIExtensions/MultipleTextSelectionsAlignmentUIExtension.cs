using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions
{
    [ClipboxExtensionAttribute]
    public class MultipleTextSelectionsAlignmentUIExtension : UIExtension
    {

        private const string DEFAULT_SEPARATOR_SETTING_KEY = nameof(DEFAULT_SEPARATOR_SETTING_KEY);

        private MultipleTextSelectionsAlignment _extensionUI;

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() > 1
                && concatinatedItems.All(item => item.ClipboardDataObjects.Any(obj => obj.ClipboardFormatName == "Rich Text Format" || obj.ClipboardFormatName == "Text" || obj.ClipboardFormatName == "CF_UNICODETEXT"));
        }

        public override void OnExtensionViewGenerating(IEnumerable<IClipboardItem> concatinatedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extensionUI = new MultipleTextSelectionsAlignment();

            string separatorTypeStr = GetSetting(DEFAULT_SEPARATOR_SETTING_KEY);
            TextConcatenateSeparators separatorType;
            if (separatorTypeStr != null)
            {
                separatorType = (TextConcatenateSeparators)Enum.Parse(typeof(TextConcatenateSeparators), separatorTypeStr);
            }
            else
            {
                separatorType = TextConcatenateSeparators.None;
            }

            _extensionUI.SetSeparatorType(separatorType);
            additionalViewElement = _extensionUI;

            _extensionUI.SeparatorChanged += _extensionUI_SeparatorChanged;
        }

        private void _extensionUI_SeparatorChanged(object sender, MultipleTextSelectionsAlignment.SeparatorEventArgs e)
        {
            SetConcatenationSeparator(e.Separator);
            SetSetting(DEFAULT_SEPARATOR_SETTING_KEY, e.Separator.ToString());
        }

        public void SetConcatenationSeparator(TextConcatenateSeparators separator)
        {
            SetParameter(FlowParameters.TextConcatenateSeparator, separator);
        }
    }
}
