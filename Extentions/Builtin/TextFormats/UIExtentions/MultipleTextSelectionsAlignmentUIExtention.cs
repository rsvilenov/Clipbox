using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Extentions.BuiltIn.TextFormats.UIExtentions
{
    [ClipboxExtention]
    public class MultipleTextSelectionsAlignmentUIExtention : UIExtention
    {

        private const string DEFAULT_SEPARATOR_SETTING_KEY = nameof(DEFAULT_SEPARATOR_SETTING_KEY);

        private MultipleTextSelectionsAlignment _extentionUI;

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() > 1
                && concatinatedItems.All(item => item.ClipboardDataObjects.Any(obj => obj.ClipboardFormatName == "Rich Text Format" || obj.ClipboardFormatName == "Text" || obj.ClipboardFormatName == "CF_UNICODETEXT"));
        }

        public override void OnExtentionViewGenerating(IEnumerable<IClipboardItem> concatinatedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extentionUI = new MultipleTextSelectionsAlignment();

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

            _extentionUI.SetSeparatorType(separatorType);
            additionalViewElement = _extentionUI;

            _extentionUI.SeparatorChanged += _extentionUI_SeparatorChanged;
        }

        private void _extentionUI_SeparatorChanged(object sender, MultipleTextSelectionsAlignment.SeparatorEventArgs e)
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
