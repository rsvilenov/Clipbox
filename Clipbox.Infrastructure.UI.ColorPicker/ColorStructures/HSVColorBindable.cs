using System;

namespace Clipbox.Infrastructure.UI.ColorPicker.ColorStructures
{
    public class HSVColorBindable : BindableObject
    {
        internal HSVColorBindable(HSVColor hsv)
        {
            Hue = hsv.H;
            Saturation = hsv.S;
            Value = hsv.V;
            HueRounded = Math.Round(hsv.H);
            SaturationPercentRounded = Math.Round(hsv.S * 100);
            ValuePercentRounded = Math.Round(hsv.V * 100);
        }

        private double _hue;

        public double Hue
        {
            get { return _hue; }
            set { SetField(ref _hue, value); }
        }

        private double _saturation;

        public double Saturation
        {
            get { return _saturation; }
            set { SetField(ref _saturation, value); }
        }

        private double _value;

        public double Value
        {
            get { return _value; }
            set { SetField(ref _value, value); }
        }

        private double _hueRounded;

        public double HueRounded
        {
            get { return _hueRounded; }
            set { SetField(ref _hueRounded, value); }
        }

        private double _saturationPercentRounded;

        public double SaturationPercentRounded
        {
            get { return _saturationPercentRounded; }
            set { SetField(ref _saturationPercentRounded, value); }
        }

        private double _valuePercentRounded;

        public double ValuePercentRounded
        {
            get { return _valuePercentRounded; }
            set { SetField(ref _valuePercentRounded, value); }
        }


        internal HSVColor GetHSV()
        {

            return new HSVColor
            {
                H = this.Hue,
                S = this.Saturation,
                V = this.Value
            };
        }
    }
}
