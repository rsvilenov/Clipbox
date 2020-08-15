using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for EnabledExtensionsSettingUIPart.xaml
    /// </summary>
    public partial class EnabledExtensionsSettingUIPart : SettingUIPart<string>
    {
        public EnabledExtensionsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public int EnabledExtensionsCount
        {
            get { return (int)GetValue(EnabledExtensionsCountProperty); }
            set { SetValue(EnabledExtensionsCountProperty, value); }
        }

        public static readonly DependencyProperty EnabledExtensionsCountProperty =
            DependencyProperty.Register(nameof(EnabledExtensionsCount), typeof(int), typeof(EnabledExtensionsSettingUIPart), new PropertyMetadata(0));


        public string EnabledExtensionsXml
        {
            get { return (string)GetValue(EnabledExtensionsXmlProperty); }
            set { SetValue(EnabledExtensionsXmlProperty, value); }
        }

        public static readonly DependencyProperty EnabledExtensionsXmlProperty =
            DependencyProperty.Register(nameof(EnabledExtensionsXml), typeof(string), typeof(EnabledExtensionsSettingUIPart), new PropertyMetadata(string.Empty));



        public override string GetSettingValue()
        {
            return EnabledExtensionsXml;
        }

        public override void SetSettingValue(string value)
        {
            EnabledExtensionsXml = value;
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


            IEnumerable<PersistableExtensionInfo> enabledExtensions = XmlSerialization.Deserialize<List<PersistableExtensionInfo>>(EnabledExtensionsXml);
            EnabledExtensionsSelector extSelector = new EnabledExtensionsSelector(enabledExtensions);

            popup.Child = extSelector;


            EventHandler<EventArgsClasses.EnabledExtensionsEventArgs> enabledExtInfoChangedEventHandler = null;

            enabledExtInfoChangedEventHandler = new EventHandler<EventArgsClasses.EnabledExtensionsEventArgs>
                ((_, dialogArgs) =>
                {
                    // re-enable the settings window
                    Window.GetWindow(this).IsEnabled = true;

                    popup.IsOpen = false;
                    extSelector.EnabledExtensionsChanged -= enabledExtInfoChangedEventHandler;

                    if (dialogArgs.EnabledExtensions != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            EnabledExtensionsXml = XmlSerialization.Serialize(dialogArgs.EnabledExtensions.ToList());

                            EnabledExtensionsCount = dialogArgs.EnabledExtensions.Count();

                            NotifySettingModified();
                        }));
                    }
                });

            extSelector.EnabledExtensionsChanged += enabledExtInfoChangedEventHandler;

            popup.IsOpen = true;

            // disable the settings window while the popup is open
            Window.GetWindow(this).IsEnabled = false;
        }
    }
}
