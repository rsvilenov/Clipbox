using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for CloudSupportSelectorSettingUIPart.xaml
    /// </summary>
    public partial class CloudSupportSelectorSettingUIPart : SettingUIPart<string>
    {
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public CloudSupportSelectorSettingUIPart()
        {
            InitializeComponent();

            AvailableCloudExtensions = new AsyncObservableCollection<CloudInfoDisplayData>();
            this.mainGrid.DataContext = this;
            this.Visibility = Visibility.Collapsed;
            IsVisibleChanged += CloudSupportSelectorSettingUIPart_IsVisibleChanged;

            OkClicked = new DelegateCommand(OkCommandHandler, () => SelectedCloudInfoDisplayData != null);
            CancelClicked = new DelegateCommand(CancelCommandHandler);
        }

        private void CancelCommandHandler()
        {
            SetIsVisible(false);
            SelectedCloudInfoDisplayData = null;
            CloudExtensionName = string.Empty;
        }

        private void OkCommandHandler()
        {
            CloudExtensionName = SelectedCloudInfoDisplayData.Name;
            SetIsVisible(false);
        }

        private void CloudSupportSelectorSettingUIPart_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                cloudSelectorPopup.IsOpen = true;
                cloudSelectorPopup.Closed += CloudSelectorPopup_Closed;
                Window.GetWindow(this).IsEnabled = false;
                LoadCloudExtensionsInfo();
            }
            else
            {
                cloudSelectorPopup.IsOpen = false;
            }
        }

        private void CloudSelectorPopup_Closed(object sender, System.EventArgs e)
        {
            Window.GetWindow(this).IsEnabled = true;
        }

        private void LoadCloudExtensionsInfo()
        {
            AvailableCloudExtensions.Clear();

            ExtensionLoader extensionLoader = new ExtensionLoader();
            var cloudSupportExtensions = extensionLoader
                .GetAllAvailableExtensions();

            foreach (ExtensionTypeInfo eti in cloudSupportExtensions)
            {
                if (!CloudSupportAttribute.IsDefinedFor(eti.ReflectionOnlyType))
                {
                    continue;
                }

                ICloudInfo cloudInfo = CloudSupportAttribute.GetCloudInfo(eti.ReflectionOnlyType);
                ImageSource imgSource = null;
                if (cloudInfo.IconImageData != null)
                {
                    using (MemoryStream ms = new MemoryStream(cloudInfo.IconImageData))
                    using (Bitmap bmp = new Bitmap(ms))
                    {
                        imgSource = bmp.ToBitmapSource();
                    }
                }

                AvailableCloudExtensions.Add(new CloudInfoDisplayData
                {
                    Description = eti.PersistableExtensionInfo.Description,
                    Name = cloudInfo.CloudServiceName,
                    Icon = imgSource
                });
            }
        }

        private AsyncObservableCollection<CloudInfoDisplayData> AvailableCloudExtensions
        {
            get { return (AsyncObservableCollection<CloudInfoDisplayData>)GetValue(AvailableCloudExtensionsProperty); }
            set { SetValue(AvailableCloudExtensionsProperty, value); }
        }

        private static readonly DependencyProperty AvailableCloudExtensionsProperty =
            DependencyProperty.Register(nameof(AvailableCloudExtensions), typeof(AsyncObservableCollection<CloudInfoDisplayData>), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(null));



        private CloudInfoDisplayData SelectedCloudInfoDisplayData
        {
            get { return (CloudInfoDisplayData)GetValue(SelectedCloudInfoDisplayDataProperty); }
            set { SetValue(SelectedCloudInfoDisplayDataProperty, value); }
        }

        private static readonly DependencyProperty SelectedCloudInfoDisplayDataProperty =
            DependencyProperty.Register(nameof(SelectedCloudInfoDisplayData), typeof(CloudInfoDisplayData), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(null));



        public string CloudExtensionName
        {
            get { return (string)GetValue(CloudExtensionNameProperty); }
            set { SetValue(CloudExtensionNameProperty, value); }
        }

        public static readonly DependencyProperty CloudExtensionNameProperty =
            DependencyProperty.Register(nameof(CloudExtensionName), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty, CloudExtensionName_PropertyChanged));

        private static void CloudExtensionName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (CloudSupportSelectorSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override string GetSettingValue()
        {
            return CloudExtensionName;
        }

        public override void SetSettingValue(string value)
        {
            CloudExtensionName = value;
        }


        private class CloudInfoDisplayData : BindableObject
        {
            private string _name;
            public string Name
            {
                get { return _name; }
                set { SetField(ref _name, value); }
            }

            private string _description;
            public string Description
            {
                get { return _description; }
                set { SetField(ref _description, value); }
            }

            private ImageSource _icon;
            public ImageSource Icon
            {
                get { return _icon; }
                set { SetField(ref _icon, value); }
            }

        }


        
    }
}
