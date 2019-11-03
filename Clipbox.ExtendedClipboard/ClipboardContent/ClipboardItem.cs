using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    internal class ClipboardItem : IClipboardItem, ICloudSynchronizable, IEditableObject
    {
        internal ClipboardItem(
            string id,
            IClipboardItemSource source,
            IEnumerable<IClipboardDataObject> clipboardDataObjects,
            ITimestampInfo timestampInfo)
        {
            Id = id;
            ClipboardDataObjects = clipboardDataObjects?.ToList();
            Source = source;

            if (timestampInfo == null)
            {
                TimestampInfo = new TimestampInfo(DateTime.Now.Ticks,
                    DateTime.UtcNow.Ticks,
                    DateTime.Now.Ticks,
                    DateTime.UtcNow.Ticks);
            }
            else
            {
                TimestampInfo = timestampInfo;
            }

            _livingObjects.Add(new ObjectInfo
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

        internal ClipboardItem(
            string id,
            IClipboardItemSource source,
            IEnumerable<IClipboardDataObject> clipboardDataObjects)
            : this(id, source, clipboardDataObjects, null)
        { }


        /// <summary>
        /// We want to preserve the order of the items and make it known to the users of the code...
        /// IEnumerable<T> is not sufficiant to ensure the consumers that the order will be the same.
        /// However, IList<IClipboardDataObject> is not covariant with IList<ClipboardDataObject>,
        /// since IList.Add() makes it theoretically possible to add other implementations of IClipboardDataObject
        /// to the collection, so that may introduce the need for the users to call .Cast<ClipboardDataObject>()...
        /// <see cref="IClipboardItem.ClipboardDataObjects"/>
        /// </summary>
        public IList<IClipboardDataObject> ClipboardDataObjects { get; internal set; }

        /// <summary>
        /// <see cref="IClipboardItem.IsShrinked"/>
        /// </summary>
        public bool IsShrinked
        {
            get
            {
                return ClipboardDataObjects?.Any(cdo => cdo.IntermediateFormatObject == null) == true;
            }
        }

        /// <summary>
        /// <see cref="IClipboardItem.IsPinned"/>
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// <see cref="IClipboardItem.IsTemporary"/>
        /// </summary>
        public bool IsTemporary { get; internal set; }

        /// <summary>
        /// Has the clipboard item collection been modified
        /// since the item was added.
        /// </summary>
        internal bool IsModified { get; private set; }

        /// <summary>
        /// <see cref="IHaveClipboardItemInfo.Source"/>
        /// </summary>
        public IClipboardItemSource Source { get; }
        
        /// <summary>
        /// <see cref="IHaveClipboardItemInfo.Id"/>
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// <see cref="IHaveClipboardItemInfo.TimestampInfo"/>
        /// </summary>
        public ITimestampInfo TimestampInfo { get; private set; }


        /// <summary>
        /// <see cref="IHaveClipboardItemInfo.Tag"/>
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// <see cref="ICloudSynchronizable.IsSynchronizedWithCloud"/>
        /// </summary>
        public bool IsSynchronizedWithCloud { get; set; }

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
        
        #region IEditableObject support

        private ClipboardItem _originalClipboardItem;
        public void BeginEdit()
        {
            _originalClipboardItem = (ClipboardItem)this.MemberwiseClone();
        }

        public void EndEdit()
        {
            IsModified = true;
            TimestampInfo = new TimestampInfo(TimestampInfo.EntryDateTicks,
                TimestampInfo.EntryDateUtcTicks,
                DateTime.Now.Ticks,
                DateTime.UtcNow.Ticks);
        }

        public void CancelEdit()
        {
            CanTransfer = _originalClipboardItem.CanTransfer;
            ClipboardDataObjects = _originalClipboardItem.ClipboardDataObjects;
            IsPinned = _originalClipboardItem.IsPinned;
            IsSynchronizedWithCloud = _originalClipboardItem.IsSynchronizedWithCloud;
            ShouldTransfer = _originalClipboardItem.ShouldTransfer;
            IsTemporary = _originalClipboardItem.IsTemporary;
            Tag = _originalClipboardItem.Tag;
        }

        #endregion

        public ClipboardItem ShallowClone()
        {
            return (ClipboardItem)this.MemberwiseClone();
        }

        ~ClipboardItem()
        {
            Debug.WriteLine($"GC-ing {Id} - {Source?.SourceWindowInfo?.Caption}");

            Debug.WriteLine($"Alive objects: {_livingObjects.Count} - {string.Join(",", _livingObjects.Select(obj => obj.Id + " " + obj.CreateDate.ToString() + Environment.NewLine).ToList<string>())}");

            if (_livingObjects.Any(obj => obj.HashCode == GetHashCode()))
            {
                _livingObjects.Remove(_livingObjects.First(d => d.HashCode == GetHashCode()));
            }
        }

        private class ObjectInfo
        {
            public int HashCode { get; set; }

            public string Id { get; set; }

            public DateTime CreateDate { get; set; }

            public string AllocationStackTrace { get; set; }
        }

        private static List<ObjectInfo> _livingObjects { get; set; } = new List<ObjectInfo>();
    }
}
