using System;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class MonitorDimensions
    {
        // monitor info
        public int MonitorWidth { get; internal set; }
        public int MonitorHeight { get; internal set; }
        public double MonitorDPI { get; internal set; }
    }
}
