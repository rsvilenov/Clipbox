using Clipbox.Win32;
using System;
using System.Windows;

namespace Clipbox.Views.Interfaces
{
    public interface ITrayContextMenu
    {
        MonitorUtils.DisplayMonitor CurrentMonitor { get; }

        void OpenAtPoint(Point p, double scaleFactor);

        void Close();

        event EventHandler Opened;
    }
}
