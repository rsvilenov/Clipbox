using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for SizeToDpiSettingUIPart.xaml
    /// </summary>
    public partial class SizeToDpiSettingUIPart : SettingUIPart<bool>
    {
        public SizeToDpiSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool SizeToDpiEnabled
        {
            get { return (bool)GetValue(SizeToDpiEnabledProperty); }
            set { SetValue(SizeToDpiEnabledProperty, value); }
        }

        public static readonly DependencyProperty SizeToDpiEnabledProperty =
            DependencyProperty.Register(nameof(SizeToDpiEnabled), typeof(bool), typeof(SizeToDpiSettingUIPart), new PropertyMetadata(false, SizeToDpiEnabled_PropertyChanged));

        private static void SizeToDpiEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SizeToDpiSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return SizeToDpiEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            SizeToDpiEnabled = value;
        }
    }
}
