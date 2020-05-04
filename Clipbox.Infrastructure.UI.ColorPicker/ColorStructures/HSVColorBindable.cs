namespace Clipbox.Infrastructure.UI.ColorPicker.ColorStructures
{
    public class HSVColorBindable : BindableObject
    {
        internal HSVColorBindable(HSVColor hsv)
        {
            Hue = hsv.H;
            Value = hsv.V;
            Saturation = hsv.S;
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
