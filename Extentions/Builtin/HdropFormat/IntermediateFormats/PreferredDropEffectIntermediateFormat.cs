using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using System;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats
{
    public class PreferredDropEffectIntermediateFormat : IIntermediateFormat
    {
        public string FormatGuid => "{8E4402B8-E9ED-4234-9E6C-B4629D673F5C}";

        public PreferredDropEffectIntermediateFormat(DropEffects dropEffect)
        {

            DropEffect = dropEffect;
        }

        public DropEffects DropEffect { get; }

        public bool IsTransferable => false;

        public ClipboardContentClass ContentClass => ClipboardContentClass.AuxilliaryData;
    }
}
