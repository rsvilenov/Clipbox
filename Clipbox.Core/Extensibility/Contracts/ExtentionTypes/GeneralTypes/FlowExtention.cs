using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Clipbox.Core.Extensibility.Enums;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public abstract class FlowExtention : ParametrizedExtention, IDisposable
    {
        /// <summary>
        /// Determines the order in which the flow extention run. 
        /// The lower the value, the higher the order - the extentions with higher values run first.
        /// </summary>
        public virtual int FlowOrder
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Allows filtering of dead clipboard items (items which are no longer paste-able)
        /// </summary>
        /// <param name="items">A reference to a clipboard item</param>
        /// <param name="clipboardFormat">The name of the clipboard format for which the housekeeping is requested</param>
        /// <returns>IHouseKeepingResult</returns>
        public virtual IHouseKeepingResult OnHousekeepingPerformed(IClipboardItem item, string clipboardFormat)
        {
            return null;
        }

        /// <summary>
        /// Returns an IHouseKeepingTask object. This is required for
        /// <see cref="OnHousekeepingPerformed"/> to be called.
        /// </summary>
        /// <param name="items">A reference to a clipboard item</param>
        /// <returns>IHouseKeepingResult</returns>
        public virtual IHouseKeepingTask RegisterHousekeepingTask()
        {
            return null;
        }

        /// <summary>
        /// Check whether modification of the items is needed
        /// </summary>
        /// <param name="items">A collection of items to combine</param>
        /// <returns>'True' if the items should be modified. If this method returns 'true', there should be
        /// modification logic impelemted in <see cref="OnPerformModifyRequested"/> in the same extention.</returns>
        public virtual bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            return false;
        }

        /// <summary>
        /// Allows implementing custom login for modifying clipboard entries before they are pasted.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public virtual IGeneratedClipboardItem OnPerformModifyRequested(IEnumerable<IClipboardItem> items)
        {
            throw new InvalidOperationException("OnQueryModify returned true, but OnPerformModifyRequested is not implemented...");
        }

        /// <summary>
        /// Allows doing a custom operation before the preview window is shown.
        /// </summary>
        /// <param name="targetWindowInfo">Info for the target window.</param>
        /// <param name="cancelPaste">If set to true, cancels the showing of the window</param>
        public virtual void OnShowQuickViewWindowRequested(IWindowInfo targetWindowInfo, ref bool cancelShow)
        { }

        /// <summary>
        /// Allows doing a custom operation before the contents are pasted.
        /// </summary>
        /// <param name="clipboardItem">The clipboard item to be pasted.</param>
        /// <param name="targetWindowInfo">Info for the target window.</param>
        /// <param name="cancelPaste">If set to true, cancels the paste operation</param>
        public virtual void OnManagedPasteRequested(IClipboardItem clipboardItem, IWindowInfo targetWindowInfo, ref bool cancelPaste)
        { }

        /// <summary>
        /// Allows doing a custom operation after the contents are pasted. 
        /// This method executes when the QuickView's paste functionality has been used. 
        /// It does not execute on native paste operations,
        /// because these operations cannot be detected reliably.
        /// </summary>
        /// <param name="clipboardItem">The pasted clipboard item.</param>
        /// <param name="targetWindowInfo">Info for the target window.</param>
        /// <param name="removeItem">If set to true, removes the current item from the list of clipboard items.</param>
        public virtual void OnManagedPasteCompleted(IClipboardItem clipboardItem, IWindowInfo targetWindowInfo, ref bool removeItem)
        { }

        /// <summary>
        /// Allows doing a custom operation before adding a copied item into the clipboard items collection.
        /// </summary>
        /// <param name="sourceWindowInfo"></param>
        /// <param name="cancelStoreToItemList"></param>
        public virtual void OnCopyRequested(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList)
        { }

        /// <summary>
        /// Allows doing a custom operation after copying.
        /// </summary>
        /// <param name="clipboardItem">The item copied. It contains info for the source window.</param>
        public virtual void OnCopyCompleted(IClipboardItem clipboardItem)
        { }

        /// <summary>
        /// Allows doing a custom operation when the extention loads.
        /// </summary>
        public virtual void OnInitialized()
        { }
        
        public virtual void Dispose()
        { }

        /// <summary>
        /// Generated clipboard object, which can be passed to ClipbpardManager.AppendFromGenerated() method.
        /// </summary>
        protected class GeneratedClipboardItem : IGeneratedClipboardItem
        {
            public void AddFormatAndData(string format, object data, ClipboardContentClass contentClass = ClipboardContentClass.Unknown)
            {
                _clipboardDataObjects.Add(new GeneratedClipboardDataObject { Format = format, Data = data, ContentClass = contentClass });
            }

            private List<IGeneratedClipboardDataObject> _clipboardDataObjects = new List<IGeneratedClipboardDataObject>();

            public IReadOnlyCollection<IGeneratedClipboardDataObject> ClipboardDataObjects
            {
                get
                {
                    return _clipboardDataObjects.AsReadOnly();
                }
                private set
                {
                    _clipboardDataObjects = value?.ToList();
                }
            }
        }

        private class GeneratedClipboardDataObject : IGeneratedClipboardDataObject
        {
            public object Data { get; set; }

            public string Format { get; set; }

            public ClipboardContentClass ContentClass { get; set; }
        }

        protected class HouseKeepingResult : IHouseKeepingResult
        {
            public bool ShouldRemoveItem { get; set; }

            public IEnumerable<int> ClipboardDataObjectsToRemove { get; set; }
        }

        protected class HouseKeepingTask : IHouseKeepingTask
        {
            public string ClipboardFormat { get; set; }

            public bool RequiresRestoredState { get; set; }

            public HouseKeepingRegularity Regularity { get; set; }
        }
    }
}
