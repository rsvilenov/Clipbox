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
            this.mainGrid.DataContext = this;
        }



        public string FirstBlacklistedPrograms
        {
            get { return (string)GetValue(FirstBlacklistedProgramsProperty); }
            set { SetValue(FirstBlacklistedProgramsProperty, value); }
        }
        
        public static readonly DependencyProperty FirstBlacklistedProgramsProperty =
            DependencyProperty.Register(nameof(FirstBlacklistedPrograms), typeof(string), typeof(BlacklistedAppsSettingUIParts), new PropertyMetadata(string.Empty));




        public int BlackListedProgramsNMore
        {
            get { return (int)GetValue(BlackListedProgramsNMoreProperty); }
            set { SetValue(BlackListedProgramsNMoreProperty, value); }
        }

        public static readonly DependencyProperty BlackListedProgramsNMoreProperty =
            DependencyProperty.Register(nameof(BlackListedProgramsNMore), typeof(int), typeof(BlacklistedAppsSettingUIParts), new PropertyMetadata(0));

        
        public string BlackListedPrograms
        {
            get { return (string)GetValue(BlackListedProgramsProperty); }
            set { SetValue(BlackListedProgramsProperty, value); }
        }
        
        public static readonly DependencyProperty BlackListedProgramsProperty =
            DependencyProperty.Register(nameof(BlackListedPrograms), typeof(string), typeof(BlacklistedAppsSettingUIParts), new PropertyMetadata(string.Empty));



        public override string GetSettingValue()
        {
            return BlackListedPrograms;
        }

        public override void SetSettingValue(string value)
        {
            BlackListedPrograms = value;
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


            IEnumerable<string> selectedAppInstallLocations = BlackListedPrograms.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            InstalledApplicationSelector appSelector = new InstalledApplicationSelector(selectedAppInstallLocations);

            popup.Child = appSelector;

            EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs> appInfoSelectedEventHandler = null;

            appInfoSelectedEventHandler = new EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs>
                ((_, dialogArgs) =>
                {
                    popup.IsOpen = false;
                    appSelector.AppInfoSelected -= appInfoSelectedEventHandler;

                    if (dialogArgs.SelectedApplications != null && dialogArgs.SelectedApplications.Any())
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            BlackListedPrograms = string.Join(";", dialogArgs.SelectedApplications
                               .Select(ia => ia.InstallationLocation).ToArray());
                            FirstBlacklistedPrograms = dialogArgs.SelectedApplications
                                .Select(ia => ia.InstallationLocation).First();
                            int selectedAppsCount = dialogArgs.SelectedApplications.Count();
                            if (selectedAppsCount > 0)
                            {
                                BlackListedProgramsNMore = selectedAppsCount - 1;
                            }

                            NotifySettingModified();
                        }));
                    }
                });

            appSelector.AppInfoSelected += appInfoSelectedEventHandler;

            popup.IsOpen = true;
        }
    }
}
