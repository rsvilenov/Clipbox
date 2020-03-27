using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for RangeSlider.xaml
    /// This control is based on Josh Twist's solution, originally posted here:
    /// http://www.thejoyofcode.com/Creating_a_Range_Slider_in_WPF_and_other_cool_tips_and_tricks_for_UserControls_.aspx
    /// </summary>
    public partial class RangeSlider : UserControl
    {
        public RangeSlider()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;

            this.Loaded += Slider_Loaded;
        }

        void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;
        }

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpperSlider.Value = Math.Max(UpperSlider.Value, LowerSlider.Value);
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerSlider.Value = Math.Min(UpperSlider.Value, LowerSlider.Value);
        }



        public DoubleCollection SliderTicks
        {
            get { return (DoubleCollection)GetValue(SliderTicksProperty); }
            set { SetValue(SliderTicksProperty, value); }
        }

        public static readonly DependencyProperty SliderTicksProperty =
            DependencyProperty.Register(nameof(SliderTicks), typeof(DoubleCollection), typeof(RangeSlider), new PropertyMetadata(default(DoubleCollection)));



        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register(nameof(LowerValue), typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register(nameof(UpperValue), typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RangeSlider), new UIPropertyMetadata(1d));
        
    }
}
