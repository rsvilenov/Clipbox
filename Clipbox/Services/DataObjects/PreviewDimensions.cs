using System;
using System.Drawing;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewDimensions : ICloneable
    {
        public int PreviewWidth { get; set; }
        public int PreviewHeight { get; set; }

        public static PreviewDimensions FromDrawingSize(Size size)
        {
            return new PreviewDimensions
            {
                PreviewHeight = size.Height,
                PreviewWidth = size.Width
            };
        }

        public object Clone()
        {
            return new PreviewDimensions
            {
                PreviewWidth = PreviewWidth,
                PreviewHeight = PreviewHeight
            };
        }
    }
}
