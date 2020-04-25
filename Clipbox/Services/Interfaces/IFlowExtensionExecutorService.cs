using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface IFlowExtensionExecutorService
    {
        void OnShowQuickViewWindowRequestedMethods(ref bool cancelShow);

        void RunOnCopyRequestedMethods(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList);

        void RunOnBeforeManagedPasteMethods(IClipboardItem currentClipboardItem, ref bool cancelPasteRequested);

        /// <summary>
        /// Executes when the QuickView's paste functionality has been used. It does not execute on native paste operations,
        /// because these operations cannot be detected reliably.
        /// </summary>
        /// <param name="currentClipboardItem"></param>
        /// <param name="removeItemFromClipboardItemsRequested"></param>
        void RunOnManagedPasteCompletedMethods(IClipboardItem currentClipboardItem, ref bool removeItemFromClipboardItemsRequested);

        void RunOnOnCopyCompletedMethods(IClipboardItem currentClipboardItem);

        IReadOnlyCollection<IHouseKeepingTask> RunRegisterHousekeepingTaskMethods();

        IHouseKeepingResult RunOnHousekeepingPerformedMethods(IHouseKeepingTask task, IClipboardItem clipboardItem);

        bool RunOnQueryModifyMethods(IEnumerable<IClipboardItem> items);

        IGeneratedClipboardItem ModifyItems(IEnumerable<IClipboardItem> items);

        void LoadParameters(IEnumerable<IFlowParameter> parameters);

        void ClearParameters();
    }
}
