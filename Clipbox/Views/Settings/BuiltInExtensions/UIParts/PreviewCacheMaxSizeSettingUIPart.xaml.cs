using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PreviewCacheMaxSizeSettingUIPart.xaml
    /// </summary>
    public partial class PreviewCacheMaxSizeSettingUIPart : SettingUIPart<int>
    {
        public PreviewCacheMaxSizeSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int PreviewCacheMaxSize
        {
            get { return (int)GetValue(PreviewCacheMaxSizeProperty); }
            set { SetValue(PreviewCacheMaxSizeProperty, value); }
        }

        public static readonly DependencyProperty PreviewCacheMaxSizeProperty =
            DependencyProperty.Register(nameof(PreviewCacheMaxSize), typeof(int), typeof(PreviewCacheMaxSizeSettingUIPart), new PropertyMetadata(10, PreviewCacheMaxSize_PropertyChanged));

        private static void PreviewCacheMaxSize_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PreviewCacheMaxSizeSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return PreviewCacheMaxSize * 1024 * 1024;
        }

        public override void SetSettingValue(int value)
        {
            PreviewCacheMaxSize = value / 1024 / 1024;
        }
    }
}
