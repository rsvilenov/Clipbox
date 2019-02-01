using System.Windows;

namespace Clipbox.Services.Interfaces
{
    public interface ITrayContextMenuService
    {
        void ShowContextMenuAtPoint(Point p);

        bool IsContextMenuOpen { get; }
    }
}
