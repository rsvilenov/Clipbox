using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class PersistableClipboardItem : SerializableClipboardItem, IHaveTransferabilityInfo, ICloudSynchronizable
    {
        public IEnumerable<PersistableClipboardDataObject> PersistableClipboardDataObjects { get; set; }

        /// <summary>
        /// <see cref="IHaveTransferabilityInfo.CanTransfer"/>
        /// </summary>
        public bool CanTransfer { get; set; }

        /// <summary>
        /// <see cref="IHaveTransferabilityInfo.ShouldTransfer"/>
        /// </summary>
        public bool ShouldTransfer { get; set; }

        /// <summary>
        /// <see cref="ICloudSynchronizable.IsSynchronizedWithCloud"/>
        /// </summary>
        public bool IsSynchronizedWithCloud { get; set; }
    }
}
