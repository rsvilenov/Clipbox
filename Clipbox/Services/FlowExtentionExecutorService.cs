using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Services.Interfaces;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Clipbox.Services
{
    public class FlowExtentionExecutorService : IFlowExtentionExecutorService, IDisposable
    {
        private List<FlowExtention> _flowExtentions;

        // TODO: find a better way to store the extention, which can perform the concatenation
        private FlowExtention _lastExtentionForModification;

        public FlowExtentionExecutorService(FlowExtentionContainer flowExtentionContainer)
        {
            InitializeFlowExtentions(flowExtentionContainer);
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void InitializeFlowExtentions(FlowExtentionContainer flowExtentionContainer)
        {
            // Load all extentions and store them for later. They have an OnInitialized() method,
            // which means that they have a state, therefore we should preserve their instances
            // for the lifetime of this controller.
            _flowExtentions = new List<FlowExtention>(flowExtentionContainer.ResolveAll());


            List<FlowExtention> failedExtentions = new List<FlowExtention>();
            foreach (FlowExtention extention in _flowExtentions)
            {
                try { extention.OnInitialized(); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(extention.OnInitialized), extention.GetType(), ex); }
            }
        }

        /// <summary>
        /// <exception>Win32Exception</exception>
        /// </summary>
        /// <returns></returns>
        private DestinationWindowInfo GetDestinationWindowInfo()
        {
            // Get the active window info. This is the window, which will act as a
            // receiver of the clipboard contents during paste.
            DestinationWindowInfo destWindowInfo = new DestinationWindowInfo();

            using (WindowUtils.WindowInfo wInfo = WindowUtils.GetForemostWindowInfo())
            {
                destWindowInfo.ApplicationPath = wInfo.ApplicationPath;
                destWindowInfo.Caption = wInfo.Caption;
                destWindowInfo.Icon = wInfo.Icon.ToByteArray(); // destWindowInfo.Icon will be disposed by WindowInfo.Dispose()
            }

            return destWindowInfo;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnBeforePasteMethods(IClipboardItem currentClipboardItem, ref bool cancelPasteRequested)
        {
            DestinationWindowInfo destWindowInfo = GetDestinationWindowInfo();

            foreach (var flowExtention in _flowExtentions)
            {
                bool cancelPaste = false;
                try { flowExtention.OnPasteRequested(currentClipboardItem, destWindowInfo, ref cancelPaste); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(flowExtention.OnPasteRequested), flowExtention.GetType(), ex); }
                if (cancelPaste)
                {
                    // set the out parameter to true
                    cancelPasteRequested = true;
                    // stop here
                    return;
                }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnOnCopyCompletedMethods(IClipboardItem currentClipboardItem)
        {
            foreach (var ext in _flowExtentions)
            {
                try { ext.OnCopyCompleted(currentClipboardItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(ext.OnCopyCompleted), ext.GetType(), ex); }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnOnPasteCompletedMethods(IClipboardItem currentClipboardItem, ref bool removeItemFromClipboardItemsRequested)
        {
            DestinationWindowInfo destWindowInfo = GetDestinationWindowInfo();

            foreach (var flowExtention in _flowExtentions)
            {
                bool removeItem = false;
                try { flowExtention.OnPasteCompleted(currentClipboardItem, destWindowInfo, ref removeItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(flowExtention.OnCopyCompleted), flowExtention.GetType(), ex); }
                // if any of the extentions says that we should remove the item, set the flag to true
                if (removeItem)
                {
                    removeItemFromClipboardItemsRequested = true;
                }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public IHouseKeepingResult RunOnHousekeepingPerformedMethods(IClipboardItem clipboardItem)
        {
            MergedHousekeepingResult mergedResult = new MergedHousekeepingResult();

            foreach (var flowExtention in _flowExtentions)
            {
                IHouseKeepingResult result = null;
                try
                {
                    result = flowExtention.OnHousekeepingPerformed(clipboardItem);
                }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(flowExtention.OnHousekeepingPerformed), flowExtention.GetType(), ex); }

                if (result != null)
                {
                    if (result.ShouldRemoveItem)
                    {
                        mergedResult.ShouldRemoveItem = true;
                    }

                    if (result.ClipboardDataObjectsToRemove != null)
                    {
                        foreach (int index in result.ClipboardDataObjectsToRemove)
                        {
                            mergedResult.AddClipboardDataObjectId(index);
                        }
                    }
                }
            }

            return mergedResult;
        }

        private class MergedHousekeepingResult : IHouseKeepingResult
        {
            public void AddClipboardDataObjectId(int index)
            {
                if (!_clipboardDataObjectsToRemove.Contains(index))
                {
                    _clipboardDataObjectsToRemove.Add(index);
                }
            }

            private List<int> _clipboardDataObjectsToRemove = new List<int>();
            
            public IEnumerable<int> ClipboardDataObjectsToRemove { get { return _clipboardDataObjectsToRemove; } }

            public bool ShouldRemoveItem { get; set; }
        }

        /// <summary>
        /// Runs the plugins, which have to be executed upon showing the quick view window.
        /// </summary>
        /// <param name="cancelShow"></param>
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void OnShowPreviewWindowRequestedMethods(ref bool cancelShow)
        {
            DestinationWindowInfo destWindowInfo = null;

            try
            {
                destWindowInfo = GetDestinationWindowInfo();
            }
            catch (Win32Exception wex)
            {
                Debug.WriteLine($"{nameof(OnShowPreviewWindowRequestedMethods)} : {wex}");
                return;
            }

            foreach (var flowExtention in _flowExtentions)
            {
                bool cancel = false;
                try { flowExtention.OnShowPreviewWindowRequested( destWindowInfo, ref cancel); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(flowExtention.OnShowPreviewWindowRequested), flowExtention.GetType(), ex); }
                // if any of the extentions says that we should not show the window, set the flag to true
                if (cancel)
                {
                    cancelShow = true;
                }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnCopyRequestedMethods(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList)
        {
            foreach (var flowExtention in _flowExtentions)
            {
                bool cancel = false;
                try { flowExtention.OnCopyRequested(sourceWindowInfo, ref cancel); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(flowExtention.OnCopyRequested), flowExtention.GetType(), ex); }
                // if any of the extentions says that we should cancel storing the item, set the flag to true
                if (cancel)
                {
                    cancelStoreToItemList = true;
                }
            }
        }


        public void Dispose()
        {
            foreach (var ext in _flowExtentions)
            {
                try { ext.Dispose(); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(ext.Dispose), ext.GetType(), ex); }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public bool RunOnQueryModifyMethods(IEnumerable<IClipboardItem> items)
        {
            _lastExtentionForModification = null;

            foreach (var ext in _flowExtentions)
            {
                try
                {
                    if (ext.OnQueryModify(items))
                    {
                        _lastExtentionForModification = ext;
                        return true;
                    }
                }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(ext.OnQueryModify), ext.GetType(), ex); }
            }

            return false;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public IGeneratedClipboardItem ModifyItems(IEnumerable<IClipboardItem> items)
        {
            if (_lastExtentionForModification == null)
            {
                throw new InvalidOperationException($"No extention for combining. Check {nameof(RunOnQueryModifyMethods)}");
            }

            return _lastExtentionForModification.OnPerformModifyRequested(items);
        }

        public void LoadParameters(IEnumerable<IFlowParameter> parameters)
        {
            foreach (var ext in _flowExtentions)
            {
                try { ext.LoadParameters(parameters); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(ext.LoadParameters), ext.GetType(), ex); }
            }
        }

        public void ClearParameters()
        {
            foreach (var ext in _flowExtentions)
            {
                try { ext.ClearParameters(); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(ext.ClearParameters), ext.GetType(), ex); }
            }
        }

        private class DestinationWindowInfo : IWindowInfo
        {
            public string ApplicationPath { get; internal set; }

            public string Caption { get; internal set; }

            public byte[] Icon { get; internal set; }
        }
    }
}
