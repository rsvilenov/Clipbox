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

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for ExcludedAppsSettingUIParts.xaml
    /// </summary>
    public partial class BlacklistedAppsSettingUIParts : SettingUIPart<string>
    {
        public BlacklistedAppsSettingUIParts()
        {
            InitializeComponent();
        }
        
        public override string GetSettingValue()
        {
            return "";
        }

        public override void SetSettingValue(string value)
        {
        }
    }
}
