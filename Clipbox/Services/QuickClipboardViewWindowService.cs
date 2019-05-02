using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class QuickClipboardViewWindowService : IQuickClipboardViewWindowService, IDisposable
    {
        private NonFocusableInteractiveWindow _quickViewWindow;

        private ClipboardViewControl _previewControl;

        private MonitorUtils.DisplayMonitor _currentMonitor;

        private SlidingTimer _sizeChangedSligingTimer;

        private ISettingsService _settingsService;

        private bool ShouldResizeToDpi()
        {
            return _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);
        }


        public QuickClipboardViewWindowService(ClipboardViewControl previewControl, ISettingsService settingsService)
        {
            _selectedItemIds = new List<string>();
            _selectedMultiItemIds = new Dictionary<string, ReadOnlyCollection<int>>();
            _parameters = new Dictionary<string, string>();
            _settingsService = settingsService;
            
            _quickViewWindow = new NonFocusableInteractiveWindow
            {
                // Don't allow the window to scale - we will do that manually.
                ScaleMode = ShouldResizeToDpi() ? DpiScaleMode.TaggedChildren : DpiScaleMode.None,
                // Allow scaling the window's contents except for the previews, which we will handle manually to avoid blurring.
                // We will mark the previews with the "noscaleable" tag which the base dpi window understands.
                //AutoScaleOnlyChildrenAccordingToDPI = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            
            _quickViewWindow.Content = previewControl;
            
            _previewControl = previewControl;

            _sizeChangedSligingTimer = new SlidingTimer();
            _sizeChangedSligingTimer.Elapsed += _sizeChangedSligingTimer_Elapsed;

            _previewControl.SelectionChanged += _previewControl_SelectionChanged;
        }

        private void _sizeChangedSligingTimer_Elapsed(object sender, EventArgs e)
        {
            CenterQuickViewWindowOnMonitor(_currentMonitor.WorkingArea);
        }

        private void _previewControl_SelectionChanged(object sender, SelectionEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        public bool IsWindowVisible
        {
            get
            {
                return _quickViewWindow.IsVisible;
            }
        }

        private Dictionary<string, string> _parameters;
        public IReadOnlyDictionary<string, string> Parameters
        {
            get
            {
                return _parameters;
            }
        }
        
        public event EventHandler<SelectionEventArgs> SelectionChanged;

        private List<string> _selectedItemIds;

        public IReadOnlyList<string> SelectedItemIds
        {
            get
            {
                return _selectedItemIds.AsReadOnly();
            }
        }

        private Dictionary<string, ReadOnlyCollection<int>> _selectedMultiItemIds;

        public IReadOnlyDictionary<string, ReadOnlyCollection<int>> SelectedMultiItemsIds
        {
            get
            {
                return new ReadOnlyDictionary<string, ReadOnlyCollection<int>>(_selectedMultiItemIds);
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

                _selectedItemIds.Clear();
                _selectedItemIds.AddRange(_previewControl.SelectedItemIds);
                _selectedMultiItemIds.Clear();
                _selectedMultiItemIds = _previewControl.SelectedMultiItemsIds.ToDictionary(x => x.Key, y => y.Value.AsReadOnly());
                _parameters.Clear();
                foreach (var option in _previewControl.Parameters)
                {
                    _parameters.Add(option.Key, option.Value);
                }

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
            IntPtr foregroundWindowHwnd = User32.GetForegroundWindow();
            _currentMonitor = MonitorUtils.GetByWindowHandle(foregroundWindowHwnd);
            bool resizeToDpi = ShouldResizeToDpi();

            _quickViewWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                double currentDisplayHeight = _currentMonitor.WorkingArea.Height;
                if (resizeToDpi)
                {
                    _quickViewWindow.ScaleMode = DpiScaleMode.TaggedChildren;
                    double scaleFactor = _currentMonitor.Dpi / 96;
                    currentDisplayHeight *= scaleFactor;
                }
                else
                {
                    _quickViewWindow.ScaleMode = DpiScaleMode.None;
                }
                
                _previewControl.SetCurrentDisplayHeight((int)currentDisplayHeight);

                SetMaxSizeToWindow();
                _quickViewWindow.SizeChanged += _quickViewWindow_SizeChanged;
                _quickViewWindow.Shown += _quickViewWindow_Shown;

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
                    SetMaxSizeToWindow();

                    CenterWindow(_quickViewWindow);
                }
            }));
        }

        private void SetMaxSizeToWindow()
        {
            //if (_quickViewWindow.Handle == IntPtr.Zero)
            //{
            //    throw new InvalidOperationException("_quickViewWindow is not yet initialized.");
            //}
            new WindowInteropHelper(_quickViewWindow).EnsureHandle();
            HwndSource source = HwndSource.FromHwnd(_quickViewWindow.Handle);
            Matrix matrix = source.CompositionTarget.TransformFromDevice;

            // bounds -20%
            double maxHeight = (_currentMonitor.WorkingArea.Height - (_currentMonitor.WorkingArea.Height * 0.2));
            double maxWidth = _currentMonitor.WorkingArea.Width - (_currentMonitor.WorkingArea.Width * 0.2);

            System.Windows.Point dpiIndependentSize =
                matrix.Transform(new System.Windows.Point(maxWidth, maxHeight));

            _quickViewWindow.MaxWidth = dpiIndependentSize.X;

            _quickViewWindow.MaxHeight = dpiIndependentSize.Y;
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

        public IEnumerable<IFlowParameter> GetParameters()
        {
            return _previewControl.GetParameters();
        }

        public void SaveChangedSettings()
        {
            _previewControl.SaveChangedSettings();
        }

        public void Dispose()
        {
            _sizeChangedSligingTimer.Dispose();
        }
    }
}
