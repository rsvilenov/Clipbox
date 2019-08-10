using Clipbox.Infrastructure.ObjectHelpers;
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

        public override int GetHashCode()
        {
            return GetHashCodeHelper
                .Start
                .Append(MonitorDPI)
                .Append(WorkAreaHeight)
                .Append(WorkAreaWidth);
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == this.GetHashCode();
        }

        public static bool operator ==(MonitorDimensions obj1, MonitorDimensions obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null))
            {
                return false;
            }

            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(MonitorDimensions obj1, MonitorDimensions obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
