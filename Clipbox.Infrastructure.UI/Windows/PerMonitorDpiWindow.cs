using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Windows
{
    /// <summary>
    /// Translated from https://code.msdn.microsoft.com/windowsdesktop/Per-Monitor-Aware-WPF-e43cde33
    /// Partially based on http://www.telerik.com/blogs/winforms-scaling-at-large-dpi-settings-is-it-even-possible-
    /// </summary>
    public class PerMonitorDpiWindow : BaseWindow
    {
        public double CurrentDPI { get; set; }
        
        public double ScaleFactor { get; set; }
        
        public DpiScaleMode ScaleMode { get; set; }
        
        public event EventHandler<DpiChangedEventArgs> DPIChanged;
        
        private NativeStructures.RECT _recommendedRectByWmDpichanged;

        private bool _hasBeenUnscaled = false;

        protected PerMonitorDpiWindow()
        {
            DpiUtils.AssertPerMonitorDPIAwareness();
            
            ContentRendered += PerMonitorDPIWindow_ContentRendered;
            SourceInitialized += PerMonitorDpiWindow_SourceInitialized;
        }

        private void PerMonitorDpiWindow_LayoutUpdated(object sender, EventArgs e)
        {
            DoScaleTransofm();
        }

        private void PerMonitorDpiWindow_SourceInitialized(object sender, EventArgs e)
        {
            UpdateScaleSettings();
        }

        private void UpdateScaleSettings()
        {
            CurrentDPI = DpiUtils.GetDPIForWindow(base.Handle);
            ScaleFactor = CurrentDPI / 96;

            // Windows scales the window automatically at first show() despite the PerMonitorDpi setting...
            // This happens when the magical "Fix scaling for apps" option in Windows is enabled.
            // And there seems to be no way to disable this behavior from within the application.
            // https://stackoverflow.com/questions/55538032/is-there-a-way-to-disable-scaling-for-all-the-wpf-application-even-if-windows-ha
            // According to MSDN, method NonClientDpiScaling should not be enabled by default in Per monitor Dpi mode.
            // Plus, once enabled, there does not seem to exist a system call for disabling it.
            // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enablenonclientdpiscaling
            // For now, I will just un-scale the window along with its contents (they both get scaled of course) if it has been automatically scaled.
            
            double? xScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11;
            double? yScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M22;
            if (!_hasBeenUnscaled
                && xScale != 1 
                && xScale == ScaleFactor 
                && xScale.HasValue 
                && yScale.HasValue)
            {
                ScaleTransform t = new ScaleTransform
                {
                    ScaleX = 1 / xScale.Value,
                    ScaleY = 1 / xScale.Value
                };

                t.Freeze();
                
                this.LayoutTransform = t;
                if (this.Content is FrameworkElement)
                {
                    (this.Content as FrameworkElement).LayoutTransform = t;
                }
                
                _hasBeenUnscaled = true;
            }
        }

        private void PerMonitorDPIWindow_ContentRendered(object sender, EventArgs e)
        {
            DoScaleTransofm();
        }

        private void DoScaleTransofm()
        {
            // Handle only DpiScaleMode.Full.
            // DpiScaleMode.Custom is handled by external code.
            if (ScaleMode == DpiScaleMode.Full)
            {
                Width = Width * ScaleFactor;
                Height = Height * ScaleFactor;

                //Update graphics and text based on the current DPI of the monitor
                ScaleContent(ScaleFactor);
            }
        }

        protected void ScaleContent(double scaleFactor)
        {
            var child = GetVisualChild(0) as FrameworkElement;
            if (child != null)
            {
                Scaling.ScaleElement(scaleFactor, child);
            }
        }
        
        // TODO: do we need to adjust the scale on Show(), after the window has been previously hidden with Hide()?
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            double oldDpi;
            switch ((uint)msg)
            {
                case NativeConstants.WmConstants.WM_DPICHANGED:
                    _recommendedRectByWmDpichanged = Marshal.PtrToStructure<NativeStructures.RECT>(lParam);
                    oldDpi = CurrentDPI;
                    var currentMonitor = MonitorUtils.GetByWindowHandle(hwnd);
                    CurrentDPI = DpiUtils.GetDPIForScreen(currentMonitor.Handle);//
                                                                                  //_currentDPI = (double)wParam;
                    ScaleFactor = CurrentDPI / 96;
                    if (oldDpi != CurrentDPI)
                    {
                        // do not execute this immediately as it may send WM_DPICHANGED again (see the telerik's article for per-monitor dpi)
                        Dispatcher.BeginInvoke((Action)(() => HandleDpiChange()));

                        DPIChanged?.Invoke(this, new DpiChangedEventArgs(CurrentDPI));
                    }

                    handled = true;
                    break;
                case NativeConstants.WmConstants.WM_SHOWWINDOW:
                    // https://docs.microsoft.com/en-us/windows/desktop/winmsg/wm-showwindow
                    // only process the messages which are result of ShowWindow()
                    // TODO: find out why we receive this message twice and upon processing of the first message, we get the old monitor from GetByWindowHandle.
                    if ((int)wParam == 1 && (int)lParam == 0)
                    {
                            //var currentMonitor2 = MonitorUtils.GetByWindowHandle(hwnd);
                            //Debug.WriteLine("WM_SHOWWINDOW : " + currentMonitor2.Handle);
                            UpdateScaleSettings();
                            DoScaleTransofm();
                    }
                    break;
            }

            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        private void HandleDpiChange()
        {
            if (ScaleMode == DpiScaleMode.Full)
            {
                WindowUtils.SetWindowsPosition(Handle, _recommendedRectByWmDpichanged.Left, _recommendedRectByWmDpichanged.Top,
                    _recommendedRectByWmDpichanged.Width,
                    _recommendedRectByWmDpichanged.Height, true);

                ScaleContent(ScaleFactor);
            }
        }
    }
}
