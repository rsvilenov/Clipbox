using Clipbox.Win32.Native;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.Types
{
    public class MetafilePictContainer
    {
        public NativeEnums.DeviceContextMapMode MappingMode { get; set; }

        public int UnitsX { get; set; }

        public int UnitsY { get; set; }

        public byte[] MetafileBits { get; set; }
    }
}
