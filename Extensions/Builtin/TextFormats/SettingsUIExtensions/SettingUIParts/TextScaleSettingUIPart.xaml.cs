using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for TextScaleSettingUIPart.xaml
    /// </summary>
    public partial class TextScaleSettingUIPart : SettingUIPart<double>
    {
        public TextScaleSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }
        
        public double ScaleFactor
        {
            get { return (double)GetValue(ScaleFactorProperty); }
            set { SetValue(ScaleFactorProperty, value); }
        }

        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(TextScaleSettingUIPart), new PropertyMetadata(1d, ScaleFactor_PropertyChanged));

        private static void ScaleFactor_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (TextScaleSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override double GetSettingValue()
        {
            return ScaleFactor;
        }

        public override void SetSettingValue(double value)
        {
            ScaleFactor = value;
        }
    }
}
