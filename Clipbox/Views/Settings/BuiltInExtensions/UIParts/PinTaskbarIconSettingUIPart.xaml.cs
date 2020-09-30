using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PinTaskbarIconSettingUIPart.xaml
    /// </summary>
    public partial class PinTaskbarIconSettingUIPart : SettingUIPart<bool>
    {
        public PinTaskbarIconSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool PinTaskbarIconEnabled
        {
            get { return (bool)GetValue(PinTaskbarIconEnabledProperty); }
            set { SetValue(PinTaskbarIconEnabledProperty, value); }
        }

        public static readonly DependencyProperty PinTaskbarIconEnabledProperty =
            DependencyProperty.Register(nameof(PinTaskbarIconEnabled), typeof(bool), typeof(PinTaskbarIconSettingUIPart), new PropertyMetadata(false, PinTaskbarIconEnabled_PropertyChanged));

        private static void PinTaskbarIconEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PinTaskbarIconSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return PinTaskbarIconEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            PinTaskbarIconEnabled = value;
        }
    }
}
