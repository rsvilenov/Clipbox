using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions.UIControls;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions
{
    [ClipboxExtension]
    public class MultipleTextSelectionsAlignmentFlowUIExtension : FlowUIExtension<MultipleTextSelectionsAlignment>
    {
        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() > 1
                && concatinatedItems.All(item => item.ClipboardDataObjects
                    .Any(obj => obj.IntermediateFormatObject is RichTextIntermediateFormat 
                        || obj.IntermediateFormatObject is TextIntermediateFormat 
                        || obj.IntermediateFormatObject is UnicodeTextIntermediateFormat));
        }

        private void _extensionUI_SeparatorChanged(object sender, MultipleTextSelectionsAlignment.SeparatorEventArgs e)
        {
            SetParameter(FlowParameters.TextConcatenateSeparator, e.Separator);

            if (GetSetting<bool>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault))
            {
                RequestSettingChange<int>(ExtensionSettingNames.DefaultSeparator, (int)e.Separator);
            }
        }

        protected override void OnExtensionUICreated(MultipleTextSelectionsAlignment extensionUIControl, IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt)
        {
            TextConcatenateSeparators separatorType = (TextConcatenateSeparators)GetSetting<int>(ExtensionSettingNames.DefaultSeparator);

            SetParameter(FlowParameters.TextConcatenateSeparator, separatorType);
            extensionUIControl.SetSeparatorType(separatorType);

            extensionUIControl.SeparatorChanged += _extensionUI_SeparatorChanged;
        }
    }
}
