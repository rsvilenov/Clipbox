using System;
using System.Windows;

namespace Clipbox.Views.Interfaces
{
    public interface ITrayContextMenu
    {
        void OpenAtPoint(Point p);

        void Close();

        event EventHandler Opened;
    }
}
