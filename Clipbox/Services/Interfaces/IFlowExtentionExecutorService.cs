using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface IFlowExtentionExecutorService
    {
        void OnShowQuickViewWindowRequestedMethods(ref bool cancelShow);

        void RunOnCopyRequestedMethods(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList);

        void RunOnBeforePasteMethods(IClipboardItem currentClipboardItem, ref bool cancelPasteRequested);

        void RunOnOnPasteCompletedMethods(IClipboardItem currentClipboardItem, ref bool removeItemFromClipboardItemsRequested);

        void RunOnOnCopyCompletedMethods(IClipboardItem currentClipboardItem);

        IReadOnlyCollection<IHouseKeepingTask> RunRegisterHousekeepingTaskMethods();

        IHouseKeepingResult RunOnHousekeepingPerformedMethods(IHouseKeepingTask task, IClipboardItem clipboardItem);

        bool RunOnQueryModifyMethods(IEnumerable<IClipboardItem> items);

        IGeneratedClipboardItem ModifyItems(IEnumerable<IClipboardItem> items);

        void LoadParameters(IEnumerable<IFlowParameter> parameters);

        void ClearParameters();
    }
}
