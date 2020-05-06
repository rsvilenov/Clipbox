using Clipbox.Infrastructure.UI.ColorPicker.ColorStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.ColorPicker.ColorConversion
{
    /// <summary>
    /// The following code comes from from ColorMinePortable
    /// https://github.com/muak/ColorMinePortable
    /// The versin here is slightly modify in order to
    /// use structure, containing the color information.
    /// </summary>


    //    The MIT License(MIT)
    //
    // Copyright(c) 2013 ColorMine.org
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.
    internal static class HsvConverter
    {
        internal static HSVColor ToColorSpace(Color color)
        {
            return ToColorSpace(new RGBColor
            {
                R = color.R,
                G = color.G,
                B = color.B
            });
        }

        internal static HSVColor ToColorSpace(RGBColor color)
        {
            HSVColor item = new HSVColor();

            var max = Max(color.R, Max(color.G, color.B));
            var min = Min(color.R, Min(color.G, color.B));

            if (Math.Abs(max - min) <= float.Epsilon)
            {
                item.H = 0d;
            }
            else
            {
                double diff = max - min;

                if (Math.Abs(max - color.R) <= float.Epsilon)
                {
                    item.H = 60d * (color.G - color.B) / diff;
                }
                else if (Math.Abs(max - color.G) <= float.Epsilon)
                {
                    item.H = 60d * (color.B - color.R) / diff + 120d;
                }
                else
                {
                    item.H = 60d * (color.R - color.G) / diff + 240d;
                }

                if (item.H < 0d)
                {
                    item.H += 360d;
                }
            }

            item.S = (max <= 0) ? 0 : 1d - (1d * min / max);
            item.V = max / 255d;

            // the rounding is not present in the original code of ColorMine
            return new HSVColor
            {
                H = Math.Round(item.H, 2, MidpointRounding.AwayFromZero),
                S = Math.Round(item.S, 2, MidpointRounding.AwayFromZero),
                V = Math.Round(item.V, 2, MidpointRounding.AwayFromZero)
            };
        }

        internal static RGBColor ToRGB(HSVColor item)
        {
            var range = Convert.ToInt32(Math.Floor(item.H / 60.0)) % 6;
            var f = item.H / 60.0 - Math.Floor(item.H / 60.0);

            var v = item.V * 255.0;
            var p = v * (1 - item.S);
            var q = v * (1 - f * item.S);
            var t = v * (1 - (1 - f) * item.S);

            switch (range)
            {
                case 0:
                    return NewRgb(v, t, p);
                case 1:
                    return NewRgb(q, v, p);
                case 2:
                    return NewRgb(p, v, t);
                case 3:
                    return NewRgb(p, q, v);
                case 4:
                    return NewRgb(t, p, v);
            }

            return NewRgb(v, p, q);
        }

        internal static Color ToColor(HSVColor item)
        {
            RGBColor rgb = ToRGB(item);
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }


        private static RGBColor NewRgb(double r, double g, double b)
        {
            // the rounding is not present in the original code of ColorMine
            return new RGBColor
            {
                R = (byte)Math.Round(r, 2, MidpointRounding.AwayFromZero),
                G = (byte)Math.Round(g, 2, MidpointRounding.AwayFromZero),
                B = (byte)Math.Round(b, 2, MidpointRounding.AwayFromZero)
            };
        }

        private static double Max(double a, double b)
        {
            return a > b ? a : b;
        }

        private static double Min(double a, double b)
        {
            return a < b ? a : b;
        }
    }
}
