using System;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class MonitorDimensions : ICloneable
    {
        // monitor info
        public int WorkAreaWidth { get; internal set; }
        public int WorkAreaHeight { get; internal set; }
        public double MonitorDPI { get; internal set; }

        public object Clone()
        {
            return new MonitorDimensions
            {
                MonitorDPI = MonitorDPI,
                WorkAreaHeight = WorkAreaHeight,
                WorkAreaWidth = WorkAreaWidth
            };
        }
    }
}
