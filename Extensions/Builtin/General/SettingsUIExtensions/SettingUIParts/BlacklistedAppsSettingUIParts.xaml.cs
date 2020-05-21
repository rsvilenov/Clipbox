using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Popup popup = new Popup
            {
                PlacementTarget = Window.GetWindow(this),
                Placement = PlacementMode.Center,
                MinWidth = 450,
                MinHeight = 200
            };

            InstalledApplicationSelector appSelector = new InstalledApplicationSelector();

            popup.Child = appSelector;

            EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs> appInfoSelectedEventHandler = null;

            appInfoSelectedEventHandler = new EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs>
                ((_, dialogArgs) =>
                {
                    popup.IsOpen = false;
                    appSelector.AppInfoSelected -= appInfoSelectedEventHandler;
                    
                });

            appSelector.AppInfoSelected += appInfoSelectedEventHandler;

            popup.IsOpen = true;
        }
    }
}
