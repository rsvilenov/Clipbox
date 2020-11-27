using Clipbox.Core;
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
    [ClipboxExtension(
        description: "Setting UI controls for text concatination (when more text entries are stored together)",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    public class MultipleTextSelectionsAlignmentFlowUIExtension : FlowUIExtension<MultipleTextSelectionsAlignment>
    {
        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() > 1
                && concatinatedItems.All(item => item.ClipboardDataObjects
                    .Any(cdo => cdo.IntermediateFormatMetadata.FormatGuid == "{D8F8C369-7F81-4138-9718-4F34EB1BD669}" // RichTextIntermediateFormat
                    || cdo.IntermediateFormatMetadata.FormatGuid == "{CAF7A257-882B-46CE-A26D-679D228EA8B7}" // UnicodeTextIntermediateFormat 
                    || cdo.IntermediateFormatMetadata.FormatGuid == "{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")); // TextIntermediateFormat
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
