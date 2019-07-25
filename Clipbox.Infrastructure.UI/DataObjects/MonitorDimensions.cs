using System;

namespace Clipbox.Infrastructure.UI.DataObjects
{
    [Serializable]
    public class MonitorDimensions : ICloneable
    {
        // monitor info
        public int WorkAreaWidth { get; set; }
        public int WorkAreaHeight { get; set; }
        public double MonitorDPI { get; set; }

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
