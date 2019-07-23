using System;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class MonitorDimensions : ICloneable
    {
        // monitor info
        public int MonitorWidth { get; internal set; }
        public int MonitorHeight { get; internal set; }
        public double MonitorDPI { get; internal set; }

        public object Clone()
        {
            return new MonitorDimensions
            {
                MonitorDPI = MonitorDPI,
                MonitorHeight = MonitorHeight,
                MonitorWidth = MonitorWidth
            };
        }
    }
}
