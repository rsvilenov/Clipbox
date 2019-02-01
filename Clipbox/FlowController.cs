using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace Clipbox
{
    public class FlowController : IDisposable
    {
        IClipboardManager _clipboardManager;

        ILowLevelKeyboardService _keyboardService;

        IQuickClipboardViewWindowService _previewWindowService;

        IFlowExtentionExecutorService _flowExtentionExecutorService;
        
        System.Timers.Timer _longCtrlVTimer;
        
        bool _hasCtrlVBeenPreviouslyPressed = false;

        bool _hasViewWindowShowingBeenRequested = false;

        IEnumerable<IFlowParameter> _parametersFromPreviewWindowService;

        public FlowController(IClipboardManager clipboardManager, 
            ILowLevelKeyboardService keyboardService,
            IFlowExtentionExecutorService flowExtentionExecutorService, 
            IQuickClipboardViewWindowService previewWindowService)
        {
            _clipboardManager = clipboardManager;
            _keyboardService = keyboardService;
            _flowExtentionExecutorService = flowExtentionExecutorService;
            _previewWindowService = previewWindowService;
            _previewWindowService.SelectionChanged += _previewWindowService_SelectionChanged;

            _parametersFromPreviewWindowService = Enumerable.Empty<IFlowParameter>();

            _clipboardManager.ItemFetching += _clipboardManager_ItemFetching;

            // IMPORTANT: if we try greater delays than the keyboard repeat delay, weard things start to happen.
            int keyboardDelayMs = 250; // this is the default keyboard delay
            if (SystemParameters.KeyboardDelay == 2) keyboardDelayMs = 500;
            if (SystemParameters.KeyboardDelay == 3) keyboardDelayMs = 1000;
            _longCtrlVTimer = new System.Timers.Timer(keyboardDelayMs);
            _longCtrlVTimer.AutoReset = false;
            _longCtrlVTimer.Elapsed += _longCtrlVTimer_Elapsed;
            
            _keyboardService.CtrlVHotkeyDown += _keyboardListener_CtrlVHotkeyDown;
            _keyboardService.ControlKeyUp += _keyboardListener_ControlKeyUp;
            _keyboardService.VKeyUp += _keyboardListener_VKeyUp;
        }

        private void _previewWindowService_SelectionChanged(object sender, SelectionEventArgs e)
        {

            IEnumerable<IClipboardItem> selectedItems = e.SelectedItemIds.Select(id => _clipboardManager.GetClipboardItem(id, restoreShrinked: false));

            // can we modify or combine the items?
            if (selectedItems.Count() > 1 && !_flowExtentionExecutorService.RunOnQueryModifyMethods(selectedItems))
            {
                e.Cancel = true;
                e.CancelMessage = "Incompatible items.";
            }
        }

        private void _clipboardManager_ItemFetching(object sender, ExtendedClipboard.EventArgs.ItemFetchingEventArgs e)
        {
            bool cancel = false;
            _flowExtentionExecutorService.RunOnCopyRequestedMethods(e.SourceWindowInfo, ref cancel);
            e.Cancel = cancel;
        }

        private void PerformHouseKeeping()
        {
#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

            lock (_clipboardManager)
            {
                IEnumerable<string> clipboardItemIds = _clipboardManager.GetAllItemIds(excludeShrinkedItems: true);
                foreach (string id in clipboardItemIds)
                {
                    IClipboardItem item = _clipboardManager.GetClipboardItem(id, restoreShrinked: false);
                    IHouseKeepingResult result = _flowExtentionExecutorService.RunOnHousekeepingPerformedMethods(item);
                    if (result != null)
                    {
                        if (result.ShouldRemoveItem)
                        {
                            _clipboardManager.RemoveClipboardItem(id, beSilent: true);
                        }
                        else if (result.ClipboardDataObjectsToRemove != null && result.ClipboardDataObjectsToRemove.Any())
                        {
                            _clipboardManager.RemoveClipbpardDataObjects(id, result.ClipboardDataObjectsToRemove.ToArray());
                        }
                    }
                }
            }
        }

        private void _keyboardListener_VKeyUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("V key is up");
            _longCtrlVTimer.Stop();

            // if the preview window has not been activated, ensure the normal behavior of Ctrl+V
            if (_hasCtrlVBeenPreviouslyPressed && !_previewWindowService.IsWindowVisible && !_hasViewWindowShowingBeenRequested)
            {
                Task.Run(new Action(() =>
                {
                    Thread.Sleep(10);
                    _keyboardService.SimulateCtrlV();
                }));
            }
        }

        private void _keyboardListener_ControlKeyUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("Ctrl key is up");


            _longCtrlVTimer.Stop();

            if (_previewWindowService.IsWindowVisible)
            {
                Debug.WriteLine("Closing preview window");
                ClosePreviewWindow();

                // do not hold the hook for long - we may get unhooked by Windows
                Task.Run(new Action(() =>
                {
                    // this simulates ctrl press and we do not want to do this while holding a hook to the ctrl key up event
                    Thread.Sleep(100); // TODO: find a better way of ensuring that DoPasteOperation() gets executed after the hook returns...

                    Debug.WriteLine("Doing paste operation");
                    bool pasteCancelled = false;
                    DoPasteOperation(out pasteCancelled);
                }));

            }

            _hasCtrlVBeenPreviouslyPressed = false;
        }

        private void _longCtrlVTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RequestOpenPreviewWindow();
        }

        private void _keyboardListener_CtrlVHotkeyDown(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("Ctrl+V is down");
            e.Cancel = true;
            
            _hasCtrlVBeenPreviouslyPressed = true;

           
            if (_previewWindowService.IsWindowVisible)
            {
                FocusNextItemInPreviewWindow();
            }
            else if (!_hasViewWindowShowingBeenRequested)
            {
                _longCtrlVTimer.Start();
            }
        }

        private void RequestOpenPreviewWindow()
        {
            bool cancel = false;
            _hasViewWindowShowingBeenRequested = true;
            _flowExtentionExecutorService.OnShowPreviewWindowRequestedMethods(ref cancel);
            if (!cancel)
            {
                // TODO: housekeeping here will slow us down
                PerformHouseKeeping();
                _previewWindowService.ShowQuickClipboardViewWindow();
            }
        }

        private void ClosePreviewWindow()
        {
            // hide the preview window
            _previewWindowService.HideQuickClipboardViewWindow();
            _hasViewWindowShowingBeenRequested = false;
            _parametersFromPreviewWindowService = _previewWindowService.GetParameters();
            _previewWindowService.SaveChangedSettings();
        }

        private void FocusNextItemInPreviewWindow()
        {
            _previewWindowService.FocusNextItem();
        }
        
        private void DoPasteOperation(out bool pasteCancelled)
        {
            if (!_previewWindowService.SelectedItemIds.Any() && !_previewWindowService.SelectedMultiItemsIds.Any())
            {
                pasteCancelled = true;
                return;
            }

            _flowExtentionExecutorService.ClearParameters();

            IEnumerable<IClipboardItem> selectedItems = _previewWindowService.SelectedItemIds.Select(id => _clipboardManager.GetClipboardItem(id, restoreShrinked: true));

            if (_previewWindowService.SelectedMultiItemsIds.Any())
            {
                IEnumerable<IClipboardItem> selecteMultiItems = _previewWindowService.SelectedMultiItemsIds.Keys.Select(id => _clipboardManager.GetClipboardItem(id, restoreShrinked: true));
                selectedItems = selectedItems.Concat(selecteMultiItems);

                var multiItemSelectionParamDictionary = new Dictionary<string, int[]>();
                foreach (IClipboardItem multiItem in selecteMultiItems)
                {
                    int[] selectedIds = _previewWindowService.SelectedMultiItemsIds.First(x => x.Key == multiItem.Id).Value.ToArray();
                    multiItemSelectionParamDictionary.Add(multiItem.Id, selectedIds);
                }

                var multiItemsParameter = new FlowParameter<Dictionary<string, int[]>>("MultiItemSelection", multiItemSelectionParamDictionary);
                _flowExtentionExecutorService.LoadParameters(new List<IFlowParameter> { multiItemsParameter });
            }

            _flowExtentionExecutorService.LoadParameters(_parametersFromPreviewWindowService);

            IClipboardItem currentItem = null;

            bool forceRemoveOnEnd = false;

            bool canModify = _flowExtentionExecutorService.RunOnQueryModifyMethods(selectedItems);
            if (!canModify && _previewWindowService.SelectedMultiItemsIds.Any())
            {
                throw new InvalidOperationException("Multi-selection items were found for which there are no flow extentions.");
            }

            // can we modify or combine the items?
            if (canModify)
            {
                IGeneratedClipboardItem generatedClipboardObject = _flowExtentionExecutorService.ModifyItems(selectedItems);
                if (generatedClipboardObject != null && generatedClipboardObject.ClipboardDataObjects != null && generatedClipboardObject.ClipboardDataObjects.Any())
                {
                    currentItem = _clipboardManager.AppendFromGenerated(generatedClipboardObject, beSilent: true, isTemporary: true);
                    forceRemoveOnEnd = true;
                }
                else
                {
                    throw new InvalidOperationException("Modifying items failed.");
                }
            }
            else if (selectedItems.Count() == 1)
            {
                currentItem = selectedItems.Single();
            }
            else
            {
                throw new InvalidOperationException("Selection of multiple incompatible objects must have been stopped by the UI already.");
            }
            
            bool cancelPasteRequested = false;

            _flowExtentionExecutorService.RunOnBeforePasteMethods(currentItem, ref cancelPasteRequested);

            if (cancelPasteRequested)
            {
                pasteCancelled = true;
                return;
            }

            // set the selected item in the clipboard
            _clipboardManager.SetCurrentItem(currentItem.Id);

            Debug.WriteLine("Simulating Ctrl+V: " + currentItem.ClipboardDataObjects.First().OriginalData.ToString());
            // simulate Ctrl+V in order to really paste the item
            _keyboardService.SimulateCtrlV();


            bool removeItemFromClipboardItemsRequested = false;

            _flowExtentionExecutorService.RunOnOnPasteCompletedMethods(currentItem, ref removeItemFromClipboardItemsRequested);

            if (removeItemFromClipboardItemsRequested || forceRemoveOnEnd)
            {
                _clipboardManager.RemoveClipboardItem(currentItem.Id, beSilent: true);
            }

            PerformHouseKeeping();

            pasteCancelled = false;
        }

        // TODO: rename?
        private void DoCopyOperation()
        {
            //IClipboardItem currentItem = _clipboardManager.GetAllItems().FirstOrDefault();

            //if (currentItem == null)
            //{
            //    return;
            //}

            //_flowExtentionExecutorService.RunOnOnCopyCompletedMethods(currentItem);
        }

        public void Dispose()
        {
            _longCtrlVTimer.Dispose();
        }
    }
}
