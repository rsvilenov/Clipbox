using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats
{
    public class PreferredDropEffectIntermediateFormat : IntermediateFormatBase
    {
        public PreferredDropEffectIntermediateFormat(DropEffects dropEffect)
        {
            DropEffect = dropEffect;
        }

        public DropEffects DropEffect { get; }

        protected override IntermediateFormatMetadata DefineMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{8E4402B8-E9ED-4234-9E6C-B4629D673F5C}",
                contentClass: ClipboardContentClass.AuxilliaryData,
                isTransferable: false);
        }
    }
}
