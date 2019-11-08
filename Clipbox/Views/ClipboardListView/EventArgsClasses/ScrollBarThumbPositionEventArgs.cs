using System;
using System.Windows;

namespace Clipbox.Views.ClipboardListView.EventArgsClasses
{
    public class ScrollBarThumbPositionEventArgs : EventArgs
    {
        public ScrollBarThumbPositionEventArgs(Rect thumbRect)
        {
            ThumbRect = thumbRect;
        }

        public Rect ThumbRect { get; }
    }
}
