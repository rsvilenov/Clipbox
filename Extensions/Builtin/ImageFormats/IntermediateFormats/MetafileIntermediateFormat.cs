using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Drawing.Imaging;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats
{
    public class MetafileIntermediateFormat :IntermediateFormatBase
    {
        public MetafileIntermediateFormat(Metafile metafile)
        {
            if (metafile == null)
            {
                throw new ArgumentNullException(nameof(metafile));
            }

            _metafile = (Metafile)metafile.Clone();
        }

        /// <summary>
        /// When we call methods like <see cref="Metafile.GetHenhmetafile()"/>
        /// the metafile object becomes invalid so it cannot be
        /// used anymore. So in order to preserve the original object.
        /// we need to return a copy, which can safely be invalidated,
        /// without affecting the state of our object.
        /// </summary>
        public Metafile GetCopyOfMetafile()
        {
            if (_metafile == null)
            {
                throw new ObjectDisposedException(nameof(MetafileIntermediateFormat));
            }

            return (Metafile)_metafile.Clone();
        }

        protected override void Dispose(bool disposing)
        {
            if (_metafile != null)
            {
                _metafile.Dispose();
                _metafile = null;
            }
        }

        private Metafile _metafile;

        protected override IntermediateFormatMetadata DefineMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{69BD2743-DADE-4B24-B3A1-5B6D73BDAE32}",
                contentClass: ClipboardContentClass.Image,
                isTransferable: false);
        }
    }
}
