using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using static Clipbox.Win32.MonitorUtils;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : AcryllicWindow
    {
        public SettingsWindow(ISettingsService settingsService, SettingsUIExtensionContainer extContainer)
        {
            _settingsService = settingsService;
            _extContainer = extContainer;

            InitializeComponent();
            _allExtensionUIItems = new List<ExtensionUIItem>();

            GeneralExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            PreviewsExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            CopyingExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            PastingExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            SecurityExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            CloudExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();

            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));


            this.tabControl.DataContext = this;

            SourceInitialized += SettingsWindow_SourceInitialized;
            Loaded += SettingsWindow_Loaded;
            Closing += SettingsWindow_Closing;
            MouseDown += SettingsWindow_MouseDown;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void SettingsWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // make sure that clicking outside of a conrol moves the focus away from it
            mainGrid.Focus();
        }

        private double GetThumbnailMaxHeight(DisplayMonitor monitor, out bool hasHeightBeenDpiScaled)
        {
            bool resizeToDpi = _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);

            double currentDisplayHeight = monitor.WorkingArea.Height;
            if (resizeToDpi)
            {
                double scaleFactor = monitor.Dpi / 96;
                currentDisplayHeight *= scaleFactor;
            }

            hasHeightBeenDpiScaled = resizeToDpi;

            return currentDisplayHeight * _settingsService.GetSetting<double>(SettingsKeys.PreviewItemHeightRatio);
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (ISettingChangeRequestor ext in _settingUIExtensions
                .Where(ext => ext is ISettingChangeRequestor))
            {
                ext.RequestSettingChange -= SettingChangeRequestor_RequestSettingChange;
            }
        }

        private List<IConfigurationRecipient> _settingUIExtensions = new List<IConfigurationRecipient>();
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<SettingsUIExtension> extensions = _extContainer.ResolveAll();
            foreach (var ext in extensions)
            {

                var configurationRecipient = (IConfigurationRecipient)ext;
                var settingChangeRequestor = (ISettingChangeRequestor)ext;
                
                IEnumerable<ISettingKey> allRegisteredKeys = ((ISettingsRegistrator)ext).GetSettingKeys();
                foreach (ISettingKey key in allRegisteredKeys)
                {
                    configurationRecipient.PutSetting(key, _settingsService.GetSetting(key));
                    _settingUIExtensions.Add(configurationRecipient);
                }
                
                IEnumerable<UIElement> extUIParts = ext.GetSettingsViewParts()
                    .Cast<UIElement>();

                var uiItem = new ExtensionUIItem(extUIParts, ext.GetType());
                switch (ext.Category)
                {
                    case SettingsCategory.General:
                        GeneralExtensionUIItems.Add(uiItem);
                        break;
                    case SettingsCategory.Previews:
                        PreviewsExtensionUIItems.Add(uiItem);
                        break;
                    case SettingsCategory.Copying:
                        CopyingExtensionUIItems.Add(uiItem);
                        break;
                    case SettingsCategory.Pasting:
                        PastingExtensionUIItems.Add(uiItem);
                        break;
                    case SettingsCategory.Security:
                        SecurityExtensionUIItems.Add(uiItem);
                        break;
                    case SettingsCategory.Cloud:
                        CloudExtensionUIItems.Add(uiItem);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid category.");
                }

                _allExtensionUIItems.Add(uiItem);
                
                UpdatePreview(ext);

                ext.ResultPreviewInvalidated += Ext_ResultPreviewInvalidated;
                settingChangeRequestor.RequestSettingChange += SettingChangeRequestor_RequestSettingChange;

            }
        }

        private void Ext_ResultPreviewInvalidated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => UpdatePreview((SettingsUIExtension)sender)));
        }

        private void UpdatePreview(SettingsUIExtension ext)
        {
            PerMonitorDpiWindow wnd = (PerMonitorDpiWindow)Window.GetWindow(this);
            DisplayMonitor monitor = MonitorUtils.GetByWindowHandle(wnd.Handle);

            double maxPreviewHeight = GetThumbnailMaxHeight(monitor, out bool isScaled);
            IPreviewData previewData = ext.GetResultPreview(new PreviewParams((int)maxPreviewHeight, (int)maxPreviewHeight, monitor.Dpi, isScaled));

            if (previewData is ImagePreviewData imgPreviewData
                && imgPreviewData.Preview is Bitmap bmp)
            {
                ExtensionUIItem extUIItem = _allExtensionUIItems
                    .Single(e => e.ExtensionType == ext.GetType());

                using (imgPreviewData)
                {
                    extUIItem.PreviewImage = bmp.ToBitmapSource();
                    extUIItem.Description = imgPreviewData.Annotation;
                }
            }
        }

        private readonly List<ExtensionUIItem> _allExtensionUIItems;

        private AsyncObservableCollection<ExtensionUIItem> GeneralExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(GeneralExtensionUIItemsProperty); }
            set { SetValue(GeneralExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty GeneralExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(GeneralExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));

        private AsyncObservableCollection<ExtensionUIItem> PreviewsExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(PreviewsExtensionUIItemsProperty); }
            set { SetValue(PreviewsExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty PreviewsExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(PreviewsExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));

        private AsyncObservableCollection<ExtensionUIItem> CopyingExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(CopyingExtensionUIItemsProperty); }
            set { SetValue(CopyingExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty CopyingExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(CopyingExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));

        private AsyncObservableCollection<ExtensionUIItem> PastingExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(PastingExtensionUIItemsProperty); }
            set { SetValue(PastingExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty PastingExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(PastingExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));

        private AsyncObservableCollection<ExtensionUIItem> SecurityExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(SecurityExtensionUIItemsProperty); }
            set { SetValue(SecurityExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty SecurityExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(SecurityExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));

        private AsyncObservableCollection<ExtensionUIItem> CloudExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(CloudExtensionUIItemsProperty); }
            set { SetValue(CloudExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty CloudExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(CloudExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));



        private class ExtensionUIItem : BindableObject
        {
            public ExtensionUIItem(IEnumerable<UIElement> extUIParts, Type extType)
            {
                _extensionUIParts = new AsyncObservableCollection<UIElement>(extUIParts);
                ExtensionType = extType;
            }

            public Type ExtensionType { get; }

            private AsyncObservableCollection<UIElement> _extensionUIParts;
            public AsyncObservableCollection<UIElement> ExtensionUIParts
            {
                get { return _extensionUIParts; }
                set { SetField(ref _extensionUIParts, value); }
            }

            private BitmapSource _previewImage;
            public BitmapSource PreviewImage
            {
                get { return _previewImage; }
                set { SetField(ref _previewImage, value); }
            }

            private string _description;
            public string Description
            {
                get { return _description; }
                set { SetField(ref _description, value); }
            }
        }

        private void SettingChangeRequestor_RequestSettingChange(object sender, Core.Extensibility.EventArgsClasses.RequestSettingChangeEventArgs e)
        {
            ((IConfigurationRecipient)sender).PutSetting(e.SettingKey, e.Value);
            _settingsService.SetSetting(e.SettingKey, e.Value);
        }

        public bool IsSettingChanged
        {
            get { return (bool)GetValue(IsSettingChangedProperty); }
            set { SetValue(IsSettingChangedProperty, value); }
        }

        public static readonly DependencyProperty IsSettingChangedProperty =
            DependencyProperty.Register(nameof(IsSettingChanged), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(false, IsSettingChecked_PropertyChanged));

        private static void IsSettingChecked_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SettingsWindow wnd = (SettingsWindow)d;
            if ((bool)e.NewValue)
            {
                wnd._settingsService.SetSetting(SettingsKeys.LocalizationCulture, "bg-BG");
            }
            else
            {
                wnd._settingsService.SetSetting(SettingsKeys.LocalizationCulture, "en-US");
            }
        }

        private void SettingsWindow_SourceInitialized(object sender, EventArgs e)
        {
            DisplayMonitor dm = MonitorUtils.GetByWindowHandle(this.Handle);

            bool resizeToDpi = _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);

            double currentDisplayHeight = dm.WorkingArea.Height;
            double currentDisplayWidth = dm.WorkingArea.Width;

            if (resizeToDpi)
            {
                double scaleFactor = dm.Dpi / 96;
                currentDisplayHeight *= scaleFactor;
                currentDisplayWidth *= scaleFactor;
            }

            this.MaxHeight = currentDisplayHeight - 10;
            this.MaxWidth = currentDisplayWidth - 10;
            
            _settingsService.SetSetting(SettingsKeys.LocalizationCulture, "bg-BG");
        }

        private ISettingsService _settingsService;

        private SettingsUIExtensionContainer _extContainer;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
