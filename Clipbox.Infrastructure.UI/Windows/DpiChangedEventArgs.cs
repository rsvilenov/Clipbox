using System;

namespace Clipbox.Infrastructure.UI.Windows
{
    public class DpiChangedEventArgs : EventArgs
    {
        public DpiChangedEventArgs(double newDPI)
        {
            NewDPI = newDPI;
            ScaleFactor = newDPI / 96.0;
        }

        public double NewDPI { get; private set; }

        public double ScaleFactor { get; private set; }
    }
}
