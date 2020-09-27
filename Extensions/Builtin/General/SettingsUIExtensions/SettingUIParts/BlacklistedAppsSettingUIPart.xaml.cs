using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for ExcludedAppsSettingUIParts.xaml
    /// </summary>
    public partial class BlacklistedAppsSettingUIPart : SettingUIPart<string>
    {
        public BlacklistedAppsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }
        
        public int BlackListedProgramsCount
        {
            get { return (int)GetValue(BlackListedProgramsCountProperty); }
            set { SetValue(BlackListedProgramsCountProperty, value); }
        }

        public static readonly DependencyProperty BlackListedProgramsCountProperty =
            DependencyProperty.Register(nameof(BlackListedProgramsCount), typeof(int), typeof(BlacklistedAppsSettingUIPart), new PropertyMetadata(0));

        
        public string BlackListedPrograms
        {
            get { return (string)GetValue(BlackListedProgramsProperty); }
            set { SetValue(BlackListedProgramsProperty, value); }
        }
        
        public static readonly DependencyProperty BlackListedProgramsProperty =
            DependencyProperty.Register(nameof(BlackListedPrograms), typeof(string), typeof(BlacklistedAppsSettingUIPart), new PropertyMetadata(string.Empty));



        public override string GetSettingValue()
        {
            return BlackListedPrograms;
        }

        public override void SetSettingValue(string value)
        {
            if (value != null)
            {
                BlackListedPrograms = string.Empty;
                BlackListedProgramsCount = 0;

                string[] paths = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string path in paths)
                {
                    // some blacklisted programs may be uninstalled already - check
                    bool doesPathExist = false;
                    try { doesPathExist = Directory.Exists(path) || File.Exists(path); } catch { }

                    if (doesPathExist)
                    {
                        BlackListedPrograms += path + ";";
                        BlackListedProgramsCount++;
                    }
                }
            }
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


            IEnumerable<string> selectedAppInstallLocations = BlackListedPrograms?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            InstalledApplicationSelector appSelector = new InstalledApplicationSelector(selectedAppInstallLocations);

            popup.Child = appSelector;
            

            EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs> appInfoSelectedEventHandler = null;

            appInfoSelectedEventHandler = new EventHandler<EventArgsClasses.InstalledApplicationInfoEventArgs>
                ((_, dialogArgs) =>
                {
                    // re-enable the settings window
                    Window.GetWindow(this).IsEnabled = true;

                    popup.IsOpen = false;
                    appSelector.AppInfoSelected -= appInfoSelectedEventHandler;

                    if (dialogArgs.SelectedApplications != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            BlackListedPrograms = string.Join(";", dialogArgs.SelectedApplications
                               .Select(ia => ia.InstallationLocation).ToArray());
                            
                            BlackListedProgramsCount = dialogArgs.SelectedApplications.Count();
                            
                            NotifySettingModified();
                        }));
                    }
                });

            appSelector.AppInfoSelected += appInfoSelectedEventHandler;

            popup.IsOpen = true;

            // disable the settings window while the popup is open
            Window.GetWindow(this).IsEnabled = false;
        }
    }
}
