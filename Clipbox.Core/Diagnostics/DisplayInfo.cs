using System;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class DisplayInfo
    {
        public double Dpi { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
