using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PutLastUsedQvItemsFirstSettingUIPart.xaml
    /// </summary>
    public partial class PutLastUsedQvItemsFirstSettingUIPart : SettingUIPart<bool>
    {
        public PutLastUsedQvItemsFirstSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool PutLastUsedQvItemsFirstEnabled
        {
            get { return (bool)GetValue(PutLastUsedQvItemsFirstEnabledProperty); }
            set { SetValue(PutLastUsedQvItemsFirstEnabledProperty, value); }
        }

        public static readonly DependencyProperty PutLastUsedQvItemsFirstEnabledProperty =
            DependencyProperty.Register(nameof(PutLastUsedQvItemsFirstEnabled), typeof(bool), typeof(PutLastUsedQvItemsFirstSettingUIPart), new PropertyMetadata(false, PutLastUsedQvItemsFirstEnabled_PropertyChanged));

        private static void PutLastUsedQvItemsFirstEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PutLastUsedQvItemsFirstSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return PutLastUsedQvItemsFirstEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            PutLastUsedQvItemsFirstEnabled = value;
        }
    }
}
