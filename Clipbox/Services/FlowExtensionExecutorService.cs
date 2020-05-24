using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Infrastructure.Threading;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;

namespace Clipbox.Services
{
    public class FlowExtensionExecutorService : IFlowExtensionExecutorService, IDisposable
    {
        private List<FlowExtension> _flowExtensions;

        private Dictionary<IHouseKeepingTask, FlowExtension> _housekeepingTasks;

        // TODO: find a better way to store the extension, which can perform the concatenation
        private FlowExtension _lastExtensionForModification;

        private ISettingsService _settingsService;

        private readonly SemaphoreSlim _maxItemStoringTasksSemaphore;

        private readonly int _onItemStoringFlowExtensionsTimeoutMs;

        public FlowExtensionExecutorService(FlowExtensionContainer flowExtensionContainer, ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _onItemStoringFlowExtensionsTimeoutMs = _settingsService.GetSetting(SettingsKeys.OnItemStoringFlowExtensionsTimeoutMs);
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            _maxItemStoringTasksSemaphore = new SemaphoreSlim(_settingsService.GetSetting(SettingsKeys.OnItemStoringFlowExtensionsMaxTasks));
            InitializeFlowExtensions(flowExtensionContainer);
            RunRegisterHousekeepingTaskMethods();
        }
        
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void InitializeFlowExtensions(FlowExtensionContainer flowExtensionContainer)
        {
            // Load all extension and store them for later. They have an OnInitialized() method,
            // which means that they have a state, therefore we should preserve their instances
            // for the lifetime of this controller.
            _flowExtensions = new List<FlowExtension>(flowExtensionContainer.ResolveAll());


            List<FlowExtension> failedExtensions = new List<FlowExtension>();
            foreach (FlowExtension extension in _flowExtensions)
            {
                // load the settings
                if (extension is IConfigurationRecipient confRecipient)
                {
                    foreach (ISettingKey settingKey in _settingsService.GetSettingKeys(extension.GetType()))
                    {
                        try { confRecipient.PutSetting(settingKey, _settingsService.GetSetting(settingKey)); }
                        catch (Exception ex) { throw new ClipboxExtensionException(nameof(confRecipient.PutSetting), extension.GetType(), ex); }
                    }
                }

                try { extension.OnInitialized(); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(extension.OnInitialized), extension.GetType(), ex); }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            Type extensionType = e.Key.TargetComponentType;
            if (!typeof(FlowExtension).IsAssignableFrom(extensionType))
            {
                return;
            }

            IConfigurationRecipient confRecipient = _flowExtensions.SingleOrDefault(ext => ext.GetType().Equals(extensionType));

            if (confRecipient == null)
            {
#if DEBUG
                throw new InvalidOperationException($"No extension for type {extensionType.Name} has been loaded. Check why.");
#else
                return;
#endif
            }

            foreach (ISettingKey settingKey in _settingsService.GetSettingKeys(extensionType))
            {
                try { confRecipient.PutSetting(settingKey, _settingsService.GetSetting(settingKey)); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(confRecipient.PutSetting), extensionType, ex); }
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

            IntPtr foremostWindowHwnd = WindowUtils.GetForemostWindowHwnd();

            using (WindowUtils.WindowInfo wInfo = WindowUtils.GetWindowInfo(foremostWindowHwnd))
            {
                destWindowInfo.ApplicationPath = wInfo.ApplicationPath;

                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(wInfo.ApplicationPath);
                destWindowInfo.ApplicationInfo = new DestinationApplicationInfo
                {
                    ApplicationPath = wInfo.ApplicationPath,
                    CompanyName = fileVersionInfo.CompanyName,
                    FileDescription = fileVersionInfo.FileDescription,
                    ProductName = fileVersionInfo.ProductName,
                    ProductVersion = fileVersionInfo.ProductVersion
                };
                destWindowInfo.Caption = wInfo.Caption;
                destWindowInfo.Icon = wInfo.Icon?.ToByteArray(); // destWindowInfo.Icon will be disposed by WindowInfo.Dispose()
            }
            
            return destWindowInfo;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnBeforeManagedPasteMethods(IClipboardItem currentClipboardItem, ref bool cancelPasteRequested)
        {
            DestinationWindowInfo destWindowInfo = GetDestinationWindowInfo();

            foreach (var flowExtension in _flowExtensions)
            {
                bool cancelPaste = false;
                try { flowExtension.OnManagedPasteRequested(currentClipboardItem, destWindowInfo, ref cancelPaste); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(flowExtension.OnManagedPasteRequested), flowExtension.GetType(), ex); }
                if (cancelPaste)
                {
                    // set the out parameter to true
                    cancelPasteRequested = true;
                    // stop here
                    break;
                }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnOnCopyCompletedMethods(IClipboardItem currentClipboardItem)
        {
            foreach (var ext in _flowExtensions)
            {
                try { ext.OnCopyCompleted(currentClipboardItem); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.OnCopyCompleted), ext.GetType(), ex); }
            }
        }
        
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnOnItemStoringMethods(IWindowInfo sourceWindowInfo, IEnumerable<IIntermediateFormat> originalIntermediateFormats, out IList<IIntermediateFormat> modifiedIntermediateFormats, ref bool cancelStoreToItem)
        {
            IList<IIntermediateFormat> modifiedIfos = new List<IIntermediateFormat>();
            bool cancel = false;

            _maxItemStoringTasksSemaphore.Wait();

            ManualResetEvent mre = new ManualResetEvent(false);
            using (ThreadExecutor te = new ThreadExecutor((cancellationToken) =>
             {
                 try
                 {

                     foreach (var ext in _flowExtensions)
                     {
                         try { ext.OnItemStoring(sourceWindowInfo, originalIntermediateFormats, ref modifiedIfos, ref cancel); }
                         catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.OnItemStoring), ext.GetType(), ex); }

                         if (cancel)
                         {
                             break;
                         }
                     }
                 }
                 finally
                 {
                     _maxItemStoringTasksSemaphore.Release();
                     mre.Set();
                 }

             }, _onItemStoringFlowExtensionsTimeoutMs))
            {
                te.Start();
                mre.WaitOne();
            }
            
            modifiedIntermediateFormats = modifiedIfos;
            cancelStoreToItem = cancel;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnManagedPasteCompletedMethods(IClipboardItem currentClipboardItem, ref bool removeItemFromClipboardItemsRequested)
        {
            DestinationWindowInfo destWindowInfo = GetDestinationWindowInfo();

            foreach (var flowExtension in _flowExtensions)
            {
                bool removeItem = false;
                try { flowExtension.OnManagedPasteCompleted(currentClipboardItem, destWindowInfo, ref removeItem); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(flowExtension.OnCopyCompleted), flowExtension.GetType(), ex); }
                // if any of the extension says that we should remove the item, set the flag to true
                if (removeItem)
                {
                    removeItemFromClipboardItemsRequested = true;
                }
            }
        }

        public IReadOnlyCollection<IHouseKeepingTask> RunRegisterHousekeepingTaskMethods()
        {
            _housekeepingTasks = new Dictionary<IHouseKeepingTask, FlowExtension>();

            foreach (var flowExtension in _flowExtensions)
            {
                IHouseKeepingTask hkTask = flowExtension.RegisterHousekeepingTask();
                if (hkTask != null)
                {
                    _housekeepingTasks.Add(hkTask, flowExtension);
                }
            }

            return _housekeepingTasks.Keys.ToList().AsReadOnly();
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public IHouseKeepingResult RunOnHousekeepingPerformedMethods(IHouseKeepingTask task, IClipboardItem clipboardItem)
        {
            MergedHousekeepingResult mergedResult = new MergedHousekeepingResult();

            if (!_housekeepingTasks.ContainsKey(task))
            {
                throw new KeyNotFoundException("The FlowExtension for this IHouseKeepingTask could not be found.");
            }

            FlowExtension flowExtension = _housekeepingTasks[task];

            IHouseKeepingResult result = null;
            try
            {
                result = flowExtension.OnHousekeepingPerformed(clipboardItem, task.ClipboardFormat);
            }
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(flowExtension.OnHousekeepingPerformed), flowExtension.GetType(), ex); }

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
        public void OnShowQuickViewWindowRequestedMethods(ref bool cancelShow)
        {
            DestinationWindowInfo destWindowInfo = null;

            try
            {
                destWindowInfo = GetDestinationWindowInfo();
            }
            catch (Win32Exception wex)
            {
                Debug.WriteLine($"{nameof(OnShowQuickViewWindowRequestedMethods)} : {wex}");
                return;
            }

            foreach (var flowExtension in _flowExtensions)
            {
                bool cancel = false;
                try { flowExtension.OnShowQuickViewWindowRequested( destWindowInfo, ref cancel); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(flowExtension.OnShowQuickViewWindowRequested), flowExtension.GetType(), ex); }
                // if any of the extension says that we should not show the window, set the flag to true
                if (cancel)
                {
                    cancelShow = true;
                    break;
                }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void RunOnCopyRequestedMethods(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItem)
        {
            foreach (var flowExtension in _flowExtensions)
            {
                bool cancel = false;
                try { flowExtension.OnCopyRequested(sourceWindowInfo, ref cancel); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(flowExtension.OnCopyRequested), flowExtension.GetType(), ex); }
                // if any of the extension says that we should cancel storing the item, set the flag to true
                if (cancel)
                {
                    cancelStoreToItem = true;
                    break;
                }
            }
        }


        public void Dispose()
        {
            foreach (var ext in _flowExtensions)
            {
                try { ext.Dispose(); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.Dispose), ext.GetType(), ex); }
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public bool RunOnQueryModifyMethods(IEnumerable<IClipboardItem> items)
        {
            _lastExtensionForModification = null;

            foreach (var ext in _flowExtensions)
            {
                try
                {
                    if (ext.OnQueryModify(items))
                    {
                        _lastExtensionForModification = ext;
                        return true;
                    }
                }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.OnQueryModify), ext.GetType(), ex); }
            }

            return false;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public IGeneratedClipboardItem ModifyItems(IEnumerable<IClipboardItem> items)
        {
            if (_lastExtensionForModification == null)
            {
                throw new InvalidOperationException($"No extension for combining. Check {nameof(RunOnQueryModifyMethods)}");
            }

            return _lastExtensionForModification.OnPerformModifyRequested(items);
        }

        public void LoadParameters(IEnumerable<IFlowParameter> parameters)
        {
            foreach (var ext in _flowExtensions)
            {
                try { ext.LoadParameters(parameters); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.LoadParameters), ext.GetType(), ex); }
            }
        }

        public void ClearParameters()
        {
            foreach (var ext in _flowExtensions)
            {
                try { ext.ClearParameters(); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(ext.ClearParameters), ext.GetType(), ex); }
            }
        }

        private class DestinationWindowInfo : IWindowInfo
        {
            public string ApplicationPath { get; internal set; }

            public string Caption { get; internal set; }

            public byte[] Icon { get; internal set; }

            public IApplicationInfo ApplicationInfo { get; internal set; }
        }

        private class DestinationApplicationInfo : IApplicationInfo
        {
            public string ApplicationPath { get; internal set; }

            public string ProductName { get; internal set; }

            public string ProductVersion { get; internal set; }

            public string FileDescription { get; internal set; }

            public string CompanyName { get; internal set; }
        }
    }
}
