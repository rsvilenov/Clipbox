using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats
{
    public class FilesystemPathsIntermediateFormat : IntermediateFormatBase
    {
        public FilesystemPathsIntermediateFormat(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException($"{nameof(paths)}");
            }

            if (!paths.Any())
            {
                throw new ArgumentException($"{nameof(paths)} does not contain any elements.");
            }

            if (paths.Any(p => p.Any(cc => char.IsControl(cc))))
            {
                throw new ArgumentException($"Some of the strings in {nameof(paths)} contain control characters.");
            }

            Paths = paths.ToList().AsReadOnly();
        }

        public IReadOnlyList<string> Paths { get; }

        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{3D2C252F-7838-49C2-9D5D-C6B4C92320D3}", 
                contentClass: ClipboardContentClass.File,
                isTransferable: false);
        }
    }
}
