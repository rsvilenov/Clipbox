using System;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.ColorPicker.EventArgsClasses
{
    public class ColorEventArgs : EventArgs
    {
        public ColorEventArgs(Color? color)
        {
            Color = color;
        }

        public Color? Color { get; }
    }
}
