using Clipbox.Core.ClipboardContent;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.Interfaces
{
    public interface IClipboardManager : IClipboardItemsDataProvider, IDisposable
    {
        // TODO: interface segregation principle...

        bool IsClipboardTrackingEnabled { get; set; }

        void EnableDisableSyncOfItem(string clipboardItemId, bool enable);

        void PinUnpinItem(string clipboardItemId, bool pin);

        void ProtectUnprotectItem(string clipboardItemId, bool protect);

        void SuspendClipboardForwarding();

        void ResumeClipboardForwarding();

        void DiscardLastClipboardItem();

        void ClearSystemClipboard();

        void RemoveClipboardItem(string id, bool beSilent = false, bool removeFromSystemClipboard = false, bool throwOnNotFound = false);

        void SetCurrentItem(string id);

        IEnumerable<string> GetAllItemIds(Func<IClipboardItem, bool> filterCondition = null);

        /// <summary>
        /// Gets an IClipboardItem
        /// </summary>
        /// <param name="id">The clipboard item id</param>
        /// <param name="restoreShrinked">Restore the shrinked parts of the item,
        /// such as the raw data. Default: false.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The id does not exist.</exception>
        IClipboardItem GetClipboardItem(string id, bool restoreShrinked = false);

        /// <summary>
        /// Tries to get a clipboard item by id
        /// </summary>
        /// <param name="id">The clipboard item id</param>
        /// <param name="clipboardItem">The clipboard item</param>
        /// <param name="restoreShrinked">Restore the shrinked parts of the item,
        /// such as the raw data. Default: false.</param>
        /// <returns>True if the item is fetched successfully</returns>
        bool TryGetClipboardItem(string id, out IClipboardItem clipboardItem, bool restoreShrinked = false);

        /// <summary>
        /// Removes all the clipboard items and clears the system clipboard
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="concatenated"></param>
        /// <param name="beSilent"></param>
        /// <param name="isTemporary">If true, do not store or synchronize this item</param>
        /// <returns></returns>
        IClipboardItem AppendFromGenerated(IGeneratedClipboardItem concatenated, bool beSilent = false, bool isTemporary = false);

        void RemoveClipbpardDataObjects(string clipboardItemId, int[] clipboardDataObjectIndices, bool beSilend = false);

        IReadOnlyList<IClipboardSyncObject> GetItemsForCloudSynchronization();

        bool TryAddItemFromCloud(byte[] itemData);

        void SetItemSynchronized(string id, bool isSynchronized = true);

        event EventHandler<ItemFetchingEventArgs> ItemFetching;

        event EventHandler<ItemStoringEventArgs> ItemStoring;

        event EventHandler<ItemAddedEventArgs> ItemAdded;

        event EventHandler<ClipboardManagerEventArgs> ItemRemoved;

        event EventHandler<ItemChangedEventArgs> ItemChanged;
    }
}
