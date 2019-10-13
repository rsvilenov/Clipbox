using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class ClipboardItem : IClipboardItem, ICloudSynchronizable
    {
        ~ClipboardItem()
        {
            Debug.WriteLine($"GC-ing {Id} - {SourceWindowInfo?.Caption}");

            Debug.WriteLine($"Alive objects: {LivingObjects.Count} - {string.Join(",", LivingObjects.Select(obj => obj.Id + " " + obj.CreateDate.ToString() + Environment.NewLine).ToList<string>())}");

            if (LivingObjects.Any(obj => obj.HashCode == GetHashCode()))
            {
                LivingObjects.Remove(LivingObjects.First(d => d.HashCode == GetHashCode()));
            }
        }

        public class ObjectInfo
        {
            public int HashCode { get; set; }

            public string Id { get; set; }

            public DateTime CreateDate { get; set; }

            public string AllocationStackTrace { get; set; }
        }

        public static List<ObjectInfo> LivingObjects { get; set; } = new List<ObjectInfo>();


        [NonSerialized]
        private IList<IClipboardDataObject> _clipboardDataObjects;
        /// <summary>
        /// We want to preserve the order of the items and make it known to the users of the code...
        /// IEnumerable<T> is not sufficiant to ensure the consumers that the order will be the same.
        /// However, IList<IClipboardDataObject> is not covariant with IList<ClipboardDataObject>,
        /// since IList.Add() makes it theoretically possible to add other implementations of IClipboardDataObject
        /// to the collection, so that may introduce the need for the users to call .Cast<ClipboardDataObject>()...
        /// </summary>
        public IList<IClipboardDataObject> ClipboardDataObjects
        {
            get { return _clipboardDataObjects; }
            internal set { _clipboardDataObjects = value; }
        }

        public IWindowInfo SourceWindowInfo { get; }

        public object Tag { get; set; }

        internal ClipboardItem(
            string id,
            IClipboardItemSource source,
            IEnumerable<IClipboardDataObject> clipboardDataObjects)
        {
            Id = id;
            ClipboardDataObjects = clipboardDataObjects?.ToList();
            Source = source;
            EntryDateTicks = DateTime.Now.Ticks;
            EntryDateUtcTicks = DateTime.UtcNow.Ticks;
            ModifiedDateTicks = DateTime.Now.Ticks;
            ModifiedDateUtcTicks = DateTime.UtcNow.Ticks;

            LivingObjects.Add(new ObjectInfo
            {
                HashCode = GetHashCode(),
                Id = Id,
                AllocationStackTrace = new StackTrace().ToString(),
                CreateDate = DateTime.Now
            });
        }

        internal ClipboardItem(
            IClipboardItemSource source,
            IEnumerable<IClipboardDataObject> clipboardDataObjects)
            : this(Guid.NewGuid().ToString(), source, clipboardDataObjects)
        { }
        
        public string Id { get; }

        public bool IsShrinked { get; internal set; }

        public long EntryDateTicks { get; internal set; }

        public long EntryDateUtcTicks { get; internal set; }

        public IClipboardItemSource Source { get; }

        public bool IsPinned { get; set; }

        public bool IsSynchronizedWithCloud { get; set; }

        /// <summary>
        /// Has the clipboard item collection been modified
        /// </summary>
        internal bool IsModified { get; set; }

        public long ModifiedDateTicks { get; set; }

        public long ModifiedDateUtcTicks { get; set; }

        public bool IsTemporary { get; internal set; }
        
        /// <summary>
        /// True of any of the ClipboardDataOnjects is transferable 
        /// (ClipboardDataOnject.IsTransferable=true).
        /// <see cref="IHaveTransferabilityInfo.CanTransfer"/>
        /// </summary>
        public bool CanTransfer { get; set; }

        /// <summary>
        /// Determines whether the item should be uploaded to the cloud.
        /// <see cref="IHaveTransferabilityInfo.ShouldTransfer"/>
        /// </summary>
        public bool ShouldTransfer { get; internal set; }
    }
}
