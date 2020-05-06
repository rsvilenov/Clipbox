using System;
using System.Globalization;

namespace Clipbox.Infrastructure.UI.ColorPicker.ColorStructures
{
    internal class RGBColor
    {
        public static RGBColor FromHexString(string hexString)
        {
            if (hexString.IndexOf('#') == -1)
            {
                throw new ArgumentException("Invalid hex string");
            }

            hexString = hexString.Replace("#", "");

            if (!int.TryParse(hexString, NumberStyles.AllowHexSpecifier, null, out int _))
            {
                throw new ArgumentException("The string does not represent a hex value.");
            }


            return new RGBColor
            {
                R = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier),
                G = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier),
                B = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier),
            };
        }

        public byte R;
        public byte G;
        public byte B;

        public string HexString => $"#{R:X2}{G:X2}{B:X2}";
    }
}
