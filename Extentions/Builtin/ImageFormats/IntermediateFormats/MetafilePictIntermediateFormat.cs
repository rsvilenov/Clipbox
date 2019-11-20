using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Win32.Native;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats
{
    public class MetafilePictIntermediateFormat : IntermediateFormatBase
    {
        public MetafilePictIntermediateFormat(byte[] metafileBytes, 
            NativeEnums.DeviceContextMapMode mappingMode,
            int unitsX,
            int unitsY)
        {
            MetafileBytes = metafileBytes;
            MappingMode = mappingMode;
            UnitsX = unitsX;
            UnitsY = unitsY;
        }

        public byte[] MetafileBytes { get; }

        public NativeEnums.DeviceContextMapMode MappingMode { get; }

        public int UnitsX { get; }

        public int UnitsY { get; }
        
        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{D77A8959-A11B-457E-892F-1D34BC925E87}",
                contentClass: ClipboardContentClass.Image,
                isTransferable: false);
        }
    }
}
