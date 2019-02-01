using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
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
        public const string SCALE_ELEMENT_TAG = "scale";

        public double CurrentDPI { get; set; }
        
        public double ScaleFactor { get; set; }
        
        public DpiScaleMode ScaleMode { get; set; }
        
        public event EventHandler<DpiChangedEventArgs> DPIChanged;
        
        private NativeStructures.RECT _recommendedRectByWmDpichanged;


        protected PerMonitorDpiWindow()
        {
            DpiUtils.AssertPerMonitorDPIAwareness();

            ContentRendered += PerMonitorDPIWindow_ContentRendered;
            SourceInitialized += PerMonitorDpiWindow_SourceInitialized;
        }
        
        private void PerMonitorDpiWindow_SourceInitialized(object sender, EventArgs e)
        {
            UpdateScaleSettings();
        }

        private void UpdateScaleSettings()
        {
            CurrentDPI = DpiUtils.GetDPIForWindow(base.Handle);
            ScaleFactor = CurrentDPI / 96;
        }

        private void PerMonitorDPIWindow_ContentRendered(object sender, EventArgs e)
        {
            DoScaleTransofm();
        }

        private void DoScaleTransofm()
        {
            if (ScaleMode == DpiScaleMode.Full)
            {
                Width = Width * ScaleFactor;
                Height = Height * ScaleFactor;

                //Update graphics and text based on the current DPI of the monitor
                ScaleContent(ScaleFactor);
            }
            else if (ScaleMode == DpiScaleMode.TaggedChildren)
            {
                ScaleChildrenWithTag(ScaleFactor);
            }
        }

        private void ScaleChildrenWithTag(double scaleFactor)
        {
            List<UIElement> children = this.GetVisualChildCollection((c) => c is FrameworkElement && (c as FrameworkElement).Tag as string == SCALE_ELEMENT_TAG, collectMatchChildren: false);
            foreach (var childToScale in children)
            {
                ScaleElement(scaleFactor, childToScale);
            }
        }

        protected void ScaleContent(double scaleFactor)
        {
            var child = GetVisualChild(0);
            ScaleElement(scaleFactor, child);
        }

        private static void ScaleElement(double scaleFactor, Visual child)
        {
            if (scaleFactor != 1.0)
            {
                var dpiScale = new ScaleTransform(scaleFactor, scaleFactor);
                child.SetValue(Window.LayoutTransformProperty, dpiScale);
            }
            else
            {
                child.SetValue(Window.LayoutTransformProperty, null);
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
            else if (ScaleMode == DpiScaleMode.TaggedChildren)
            {
                ScaleChildrenWithTag(ScaleFactor);
            }
        }
    }
}
