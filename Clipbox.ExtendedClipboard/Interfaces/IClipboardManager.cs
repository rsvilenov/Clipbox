using Clipbox.Core.ClipboardContent;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;

namespace Clipbox.ExtendedClipboard.Interfaces
{
    public interface IClipboardManager : IDisposable
    {
        // TODO: interface segregation principle...

        void EnableDisableSyncOfItem(string clipboardItemId, bool enable);

        void PinUnpinItemItem(string clipboardItemId, bool pin);

        void SuspendClipboardForwarding();

        void ResumeClipboardForwarding();

        void DiscardLastClipboardItem();

        void ClearSystemClipboard();

        void RemoveClipboardItem(string id, bool beSilent = false, bool removeFromSystemClipboard = false);

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

        IReadOnlyList<ClipboardSyncObject> GetItemsForCloudSynchronization();

        void AddItemFromCloud(byte[] itemData);

        void SetItemSynchronized(string id);

        event EventHandler<ItemFetchingEventArgs> ItemFetching;

        event EventHandler<ClipboardManagerEventArgs> ItemAdded;

        event EventHandler<ClipboardManagerEventArgs> ItemRemoved;

        event EventHandler<ClipboardManagerEventArgs> ItemChanged;
    }
}
