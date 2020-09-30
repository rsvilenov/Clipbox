using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for EnableSearchSettingUIPart.xaml
    /// </summary>
    public partial class EnableSearchSettingUIPart : SettingUIPart<bool>
    {
        public EnableSearchSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool SearchEnabled
        {
            get { return (bool)GetValue(SearchEnabledProperty); }
            set { SetValue(SearchEnabledProperty, value); }
        }

        public static readonly DependencyProperty SearchEnabledProperty =
            DependencyProperty.Register(nameof(SearchEnabled), typeof(bool), typeof(EnableSearchSettingUIPart), new PropertyMetadata(false, SearchEnabled_PropertyChanged));

        private static void SearchEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EnableSearchSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return SearchEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            SearchEnabled = value;
        }
    }
}
