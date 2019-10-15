using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class QuickClipboardViewWindowService : ClipboardViewWindowServiceBase, IQuickClipboardViewWindowService, IDisposable
    {
        private NonFocusableInteractiveWindow _quickViewWindow;

        private QuickViewClipboardContents _previewControl;

        private MonitorUtils.DisplayMonitor _currentMonitor;

        private SlidingTimer _sizeChangedSligingTimer;

        private ISettingsService _settingsService;

        // used for storing the original window's opacity when preloading the window (it gets shown with Opacity=0.0)
        private double _quickViewWindowOpacity;

        private bool ShouldResizeToDpi()
        {
            return _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);
        }


        public QuickClipboardViewWindowService(QuickViewClipboardContents previewControl, ISettingsService settingsService)
        {
            _settingsService = settingsService;

            SelectedItemIds = new List<string>();
            SelectedMultiItemIds = new Dictionary<string, IReadOnlyCollection<int>>();

            _quickViewWindow = new NonFocusableInteractiveWindow
            {
                // Don't allow the window to scale - we will do that manually.
                ScaleMode = ShouldResizeToDpi() ? DpiScaleMode.Custom : DpiScaleMode.None,
                // Allow scaling the window's contents except for the previews, which we will handle manually to avoid blurring.
                // We will mark the previews with the "noscaleable" tag which the base dpi window understands.
                //AutoScaleOnlyChildrenAccordingToDPI = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // initialize the window so that it starts listening to dpi changes from before is is first shown
            new WindowInteropHelper(_quickViewWindow).EnsureHandle();

            _quickViewWindow.Content = previewControl;

            _previewControl = previewControl;

            var primaryMonitor = MonitorUtils.GetPrimaryMonitor();
            _previewControl.SetMonitorDimensions(new MonitorDimensions
            {
                MonitorDPI = primaryMonitor.Dpi,
                WorkAreaHeight = primaryMonitor.WorkingArea.Height,
                WorkAreaWidth = primaryMonitor.WorkingArea.Width
            });


            _previewControl.LoadWrappedPinnedItems();

            _sizeChangedSligingTimer = new SlidingTimer();
            _sizeChangedSligingTimer.Elapsed += _sizeChangedSligingTimer_Elapsed;
            
            _previewControl.CombinabilityCheckRequested += _previewControl_CombinabilityCheckRequested;
            _previewControl.HousekeepingRequested += _previewControl_HousekeepingRequested;

            PreloadQuickViewWindow();

        }

        public IReadOnlyCollection<string> SelectedItemIds { get; set; }

        public IReadOnlyDictionary<string, IReadOnlyCollection<int>> SelectedMultiItemIds { get; set; }

        private void _previewControl_HousekeepingRequested(object sender, HousekeepingEventArgs e)
        {
            base.RaiseHousekeepingRequestedEvent(e);
        }

        private void PreloadQuickViewWindow()
        {
            _quickViewWindow.ScaleMode = ShouldResizeToDpi() ? DpiScaleMode.Custom : DpiScaleMode.None;

            MonitorUtils.DisplayMonitor primaryMonitor = MonitorUtils.GetPrimaryMonitor();

            _previewControl.SetMonitorDimensions(new MonitorDimensions
            {
                MonitorDPI = primaryMonitor.Dpi,
                WorkAreaHeight = primaryMonitor.WorkingArea.Height,
                WorkAreaWidth = primaryMonitor.WorkingArea.Width
            });

            _quickViewWindowOpacity = _quickViewWindow.Opacity;
            _quickViewWindow.Opacity = 0.0;
            _quickViewWindow.Shown += _quickViewWindow_ShownPreload;
            _quickViewWindow.Show();
        }

        private void _quickViewWindow_ShownPreload(object sender, EventArgs e)
        {
            _quickViewWindow.Shown -= _quickViewWindow_ShownPreload;

            _quickViewWindow.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => 
            {

                _quickViewWindow.Hide();
                _quickViewWindow.Opacity = _quickViewWindowOpacity;

            }));
        }

        private void _sizeChangedSligingTimer_Elapsed(object sender, EventArgs e)
        {
            CenterQuickViewWindowOnMonitor(_currentMonitor.WorkingArea);
        }
        
        private void _previewControl_CombinabilityCheckRequested(object sender, CombinabilityEventArgs e)
        {
            base.RaiseCombinabilityCheckRequestedEvent(e);
        }

        public bool IsWindowVisible
        {
            get
            {
                return _quickViewWindow.IsVisible;
            }
        }

        public void FocusNextItem()
        {
            _quickViewWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                _previewControl.FocusNextItem();
            }));
        }

        public void HideQuickClipboardViewWindow()
        {
            _quickViewWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                _quickViewWindow.SizeChanged -= _quickViewWindow_SizeChanged;
                _quickViewWindow.Shown -= _quickViewWindow_Shown;

                SelectedItemIds = _previewControl.GetSelectedItemIds();
                SelectedMultiItemIds = _previewControl.GetSelectedMultiItemsIds();
                
                // clear the state of the control and its collections
                _previewControl.ResumeDefaultState();

                // wait for all controls to become idle (e.g. the listview should unselect all selected items) and then close
                _previewControl.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => 
                { _quickViewWindow.Hide(); }));
#if DEBUG
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
#endif
            }));
        }

        public void ShowQuickClipboardViewWindow()
        {
            _previewControl.ClearParameters();

            _currentMonitor = MonitorUtils.GetByForegroundWindow();
            bool resizeToDpi = ShouldResizeToDpi();

            _quickViewWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                _quickViewWindow.ScaleMode = resizeToDpi ? DpiScaleMode.Custom : DpiScaleMode.None;

                _quickViewWindow.SizeChanged += _quickViewWindow_SizeChanged;
                _quickViewWindow.Shown += _quickViewWindow_Shown;

                _previewControl.SetMonitorDimensions(new MonitorDimensions
                {
                    MonitorDPI = _currentMonitor.Dpi,
                    WorkAreaHeight = _currentMonitor.WorkingArea.Height,
                    WorkAreaWidth = _currentMonitor.WorkingArea.Width
                });
                
                _quickViewWindow.Show();

            }));
        }

        private void _quickViewWindow_Shown(object sender, EventArgs e)
        {
            CenterQuickViewWindowOnMonitor(_currentMonitor.WorkingArea);
        }

        private void _quickViewWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _sizeChangedSligingTimer.Slide();
        }
        
        private void CenterQuickViewWindowOnMonitor(Rectangle monitorBounds)
        {
            _quickViewWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                if (_quickViewWindow.Handle != IntPtr.Zero)
                {
                    CenterWindow(_quickViewWindow);
                }
            }));
        }

        private void CenterWindow(PerMonitorDpiWindow window)
        {
            // https://stackoverflow.com/questions/4019831/how-do-you-center-your-main-window-in-wpf

            //find out if our app is being scaled by the monitor
            PresentationSource source = PresentationSource.FromVisual(window);
            double dpiScaling = (source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1);

            //get the available area of the monitor
            Rectangle workArea = _currentMonitor.WorkingArea;
            var workAreaWidth = (int)Math.Floor(workArea.Width * dpiScaling);
            var workAreaHeight = (int)Math.Floor(workArea.Height * dpiScaling);

            Rectangle windowRectangle = WindowUtils.GetWindowRectangle(window.Handle);
            //move to the centre
            window.Left = (((workAreaWidth - (windowRectangle.Width * dpiScaling)) / 2) + (workArea.Left * dpiScaling));
            window.Top = (((workAreaHeight - (windowRectangle.Height * dpiScaling)) / 2) + (workArea.Top * dpiScaling));
        }

        private System.Windows.Point RealPixelsToWpf(Window w, System.Windows.Point p)
        {
            var t = PresentationSource.FromVisual(w).CompositionTarget.TransformFromDevice;
            return t.Transform(p);
        }

        public override IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            return _previewControl.GetFlowParameters();
        }

        public void SaveChangedSettings()
        {
            _previewControl.SaveChangedSettings();
        }
        
        public void LoadClipboardItem(string clipboardItemId)
        {
            _previewControl.LoadItem(clipboardItemId);
        }

        public void Dispose()
        {
            _sizeChangedSligingTimer.Dispose();

            // TODO: it looks like we do not need these
            _previewControl.CombinabilityCheckRequested -= _previewControl_CombinabilityCheckRequested;
            _previewControl.HousekeepingRequested -= _previewControl_HousekeepingRequested;
        }
    }
}
