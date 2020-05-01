using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for SaveLastSeparatorSettingUIPart.xaml
    /// </summary>
    public partial class SaveLastSeparatorSettingUIPart : SettingUIPart<bool>
    {
        public SaveLastSeparatorSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsSaveSeparatorToggled
        {
            get { return (bool)GetValue(IsSaveSeparatorToggledProperty); }
            set { SetValue(IsSaveSeparatorToggledProperty, value); }
        }

        public static readonly DependencyProperty IsSaveSeparatorToggledProperty =
            DependencyProperty.Register(nameof(IsSaveSeparatorToggled), typeof(bool), typeof(SaveLastSeparatorSettingUIPart), new PropertyMetadata(false, IsSaveSeparatorToggled_PropertyChanged));

        private static void IsSaveSeparatorToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SaveLastSeparatorSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsSaveSeparatorToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsSaveSeparatorToggled = value;
        }
    }
}
