using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.ViewModels
{
    public abstract class ClipboardContentsViewModelBase : BindableObject
    {
        public ICommand DeleteClipboardEntry { get; set; }

        public ICommand PinUnpinClipboardEntry { get; set; }

        private ConcurrentBag<string> _itemIdsUndergoingDeletion;

        private AsyncObservableCollection<ClipboardViewItemWrapper> _clipboardViewItemWrappers;

        public AsyncObservableCollection<ClipboardViewItemWrapper> ClipboardViewItemWrappers
        {
            get { return _clipboardViewItemWrappers; }
            set { SetField(ref _clipboardViewItemWrappers, value); }
        }

        private ClipboardItemsLoadingStage _clipboardItemLoadingStage;

        public ClipboardItemsLoadingStage ClipboardItemsLoadingStage
        {
            get { return _clipboardItemLoadingStage; }
            set { SetField(ref _clipboardItemLoadingStage, value); }
        }

        protected IClipboardManager ClipboardManager { get; private set; }
        protected ISettingsService SettingsService { get; private set; }
        protected IPreviewGenerationService PreviewGenerationService { get; private set; }
        

        public ClipboardContentsViewModelBase(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService)
        {
            ClipboardManager = clipboardManager;
            SettingsService = settingsService;
            PreviewGenerationService = previewGenerationService;

            _itemIdsUndergoingDeletion = new ConcurrentBag<string>();

            DeleteClipboardEntry = new DelegateCommand<ClipboardViewItem>(DeleteClipboardEntryHandler, 
                /* disable the command until all deletion operations execute (it should be just one, so do it just in case... 
                 * and prevent the possibility of the user to click on the button twice in a row) */
                (item) => !_itemIdsUndergoingDeletion.Any());

            // wait for the binding operations to complete,
            // so we can get the changed value of the IsPinned property
            PinUnpinClipboardEntry = new DelegateCommand<ClipboardViewItem>(PinUnpinClipboardEntryHandler,
                dispatcherPriority: DispatcherPriority.ContextIdle);

            ClipboardViewItemWrappers = new AsyncObservableCollection<ClipboardViewItemWrapper>();

            clipboardManager.ItemRemoved += ClipboardManager_ItemRemoved;
        }

        private void ClipboardManager_ItemRemoved(object sender, ClipboardManagerEventArgs e)
        {
            RemoveItemWrapper(e.ItemId);
        }

        public ClipboardViewItemWrapper GetViewItemWrapper(string clipboardItemId)
        {
            var restoredClipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
            return _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
        }

        public ClipboardViewItem GetViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            var restoredClipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
            PreviewDataTransferObject pdto = PreviewGenerationService.GetPreviewDataTransferObject(restoredClipboardItem, monitorDimensions);
            return ClipboardViewItemFactory.CreateFromPreviewDataTransferObject(restoredClipboardItem, pdto);
        }
        
        [Obsolete("Use GetViewItemWrapper, GetViewItem and TryGetPreviewDimensions from within the view " +
            "and use the dispatcher background priority for assigning the bindable properties to the wrapper" +
            ", as running all three in a row synchronously and not in the dispatcher backgroud thread causes the UI to update painfully slow.")]
        public void LoadViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            ClipboardViewItemWrapper wrapper = GetViewItemWrapper(clipboardItemId);
            wrapper.ViewItem = GetViewItem(clipboardItemId, monitorDimensions);

            if (TryGetPreviewDimensions(monitorDimensions, wrapper, out ClipboardViewItemDimensionInfo cvItemDimInfo))
            {
                wrapper.DimensionInfo = cvItemDimInfo;
            }

            wrapper.IsViewItemLoaded = true;
        }

        public void UnloadViewItem(string clipboardItemId)
        {
            var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
            wrapper.ViewItem = null;
            wrapper.IsViewItemLoaded = false;
        }

        public void LoadAndWrapItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds, bool pinnedOnly = false)
        {
            List<IClipboardItem> clipboardItems = new List<IClipboardItem>();
            foreach (string clipboardItemId in clipboardItemIds)
            {
                IClipboardItem clipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId);
                clipboardItems.Add(clipboardItem);
                if (pinnedOnly && !clipboardItem.IsPinned)
                {
                    continue;
                }

                var wrapper = new ClipboardViewItemWrapper(clipboardItem);

                if (TryGetPreviewDimensions(monitorDimensions, wrapper, out ClipboardViewItemDimensionInfo cvItemDimInfo))
                {
                    wrapper.DimensionInfo = cvItemDimInfo;
                }

                ClipboardViewItemWrappers.Add(wrapper);
            }
        }
        
        public bool TryGetPreviewDimensions(MonitorDimensions monitorDimensions, 
            ClipboardViewItemWrapper wrapper,
            out ClipboardViewItemDimensionInfo cvItemDimInfo)
        {
            cvItemDimInfo = null;

            if (PreviewGenerationService.TryGetQuickPreviewDimentionInfo(wrapper.Id, monitorDimensions, out CalibratedPreviewDimentionInfo cpdi))
            {
                IEnumerable<ClipboardViewSubItemDimension> dimensions = cpdi.PreviewDimentions
                    .Select(pd =>
                        new ClipboardViewSubItemDimension
                        {
                            PreviewHeight = pd.PreviewHeight,
                            PreviewWidth = pd.PreviewWidth
                        });

                cvItemDimInfo = new ClipboardViewItemDimensionInfo
                {
                    IsViewItemGroup = cpdi.IsViewItemGroup,
                    Dimensions = new AsyncObservableCollection<ClipboardViewSubItemDimension>(dimensions),
                    NumberOfItemsWithoutPreview = cpdi.NumberOfItemsWithoutPreview
                };

                return true;
            }

            return false;
        }

        public void RemoveClipboardItemWrappers(IEnumerable<string> clipboardItemIds)
        {
            var wrappersToRemove = ClipboardViewItemWrappers
                .Where(w => clipboardItemIds.Contains(w.Id))
                .ToList();

            foreach (var wrapper in wrappersToRemove)
            {
                ClipboardViewItemWrappers.Remove(wrapper);
            }
        }

        private void PinUnpinClipboardEntryHandler(ClipboardViewItem obj)
        {
            ClipboardManager.PinUnpinItemItem(obj.ClipboardItemId, obj.IsPinned);
        }

        private void DeleteClipboardEntryHandler(ClipboardViewItem parameter)
        {
            // block consequent executions of the command until the entire delete operation is executed
            _itemIdsUndergoingDeletion.Add(parameter.ClipboardItemId);
            ClipboardManager.RemoveClipboardItem(parameter.ClipboardItemId);
        }

        protected bool RemoveItemWrapper(string clipboardItemId)
        {
            bool success = false;
            var itemToRemove = ClipboardViewItemWrappers.SingleOrDefault(item => item.Id == clipboardItemId);
            if (itemToRemove != null)
            {
                ClipboardViewItemWrappers.Remove(itemToRemove);
                success = true;
            }

            // empty the concurrent bag, so that we can enable delete command again
            while (!_itemIdsUndergoingDeletion.IsEmpty)
            {
                _itemIdsUndergoingDeletion.TryTake(out string itemId);
            }

            return success;
        }


    }
}
