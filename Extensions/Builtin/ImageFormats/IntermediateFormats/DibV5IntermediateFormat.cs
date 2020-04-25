using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats
{
    public class DibV5IntermediateFormat : IntermediateFormatBase
    {
        public DibV5IntermediateFormat(byte[] dibV5bytes)
        {
            if (dibV5bytes == null)
            {
                throw new ArgumentNullException(nameof(dibV5bytes));
            }

            _dibV5bytes = dibV5bytes.ToArray();
        }

        private readonly byte[] _dibV5bytes;

        public byte[] GetDibV5Bytes()
        {
            return _dibV5bytes.ToArray();
        }
        
        protected override IntermediateFormatMetadata DefineMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{CFCA3819-C2EF-4F94-B9B7-49D402CA0652}",
                contentClass: ClipboardContentClass.Image,
                isTransferable: false);
        }
    }
}
