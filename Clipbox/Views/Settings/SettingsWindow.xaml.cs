using Clipbox.Core;
using Clipbox.Core.Configuration;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.CustomControls;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views.Dialog;
using Clipbox.Views.Dialog.Enums;
using Clipbox.Views.Dialog.EventArgsClasses;
using Clipbox.Views.Settings.BuiltInExtensions;
using Clipbox.Views.Settings.Interfaces;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Clipbox.Win32.MonitorUtils;

namespace Clipbox.Views.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : WindowFrame, ICanResolveDependentExtensions
    {
        public SettingsWindow(ISettingsService settingsService, SettingsUIExtensionContainer extContainer)
        {
            _settingsService = settingsService;
            _extContainer = extContainer;

            InitializeComponent();

            ScaleMode = DpiScaleMode.Full;

            SetWindowTitle();
            
            _allExtensionUIItems = new List<ExtensionUIItem>();

            GeneralExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            PreviewsExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            CopyingExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            PastingExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            SecurityExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            CloudExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();
            AdvancedExtensionUIItems = new AsyncObservableCollection<ExtensionUIItem>();

            this.tabControl.DataContext = this;
            
            Loaded += SettingsWindow_Loaded;
            Closing += SettingsWindow_Closing;
            MouseDown += SettingsWindow_MouseDown;
            Shown += SettingsWindow_Shown;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, Services.EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.LocalizationCulture)
            {
                SetWindowTitle();
            }
        }

        private void SetWindowTitle()
        {
            TranslationSource translationSource = TranslationSourcePool.Instance.GetSource(Assembly.GetExecutingAssembly());
            Title = $"{ProductConstants.PRODUCT_NAME} {translationSource.GetTranslation("LBL_SETTINGS_WINDOW_TITLE_SETTINGS_PART")}";
        }

        private void SettingsWindow_Shown(object sender, EventArgs e)
        {
            SizeChanged += SettingsWindow_SizeChanged;
        }

        double _selectedTabItemHorizontalTemplateWidth;
        private void SettingsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var tabItemContainer = (tabControl.SelectedItem as TabItem).Content as ScrollViewer;
            var tabItemContents = tabItemContainer.Content as FrameworkElement;

            if (!UseInlinedPreviews)
            {
                _selectedTabItemHorizontalTemplateWidth = tabItemContents.ActualWidth;
            }

            // use the vertical template if the content width exceeds the available space
            UseInlinedPreviews = _selectedTabItemHorizontalTemplateWidth > tabItemContainer.ActualWidth;
        }

        
        private void SettingsWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // make sure that clicking outside of a conrol moves the focus away from it
            tabControl.Focus();
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
            foreach (ISettingsUIComponent ext in _settingUIExtensions)
            {
                if (ext is ISettingChangeRequestor settingChangeRequestor)
                {
                    settingChangeRequestor.RequestSettingChange -= SettingChangeRequestor_RequestSettingChange;
                }

                ext.ResultPreviewInvalidated -= Ext_ResultPreviewInvalidated;
            }

            _settingUIExtensions.Clear();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<ISettingsUIComponent> builtInSettingExtensions = new List<ISettingsUIComponent>
            {
                new GeneralBuiltInSettingsUIExtension(),
                new AdvancedBuiltInSettingsUIExtension(),
                new CloudBuiltInSettingsUIExtension(),
                new PreviewsBuiltInSettingsUIExtension()
            };

            LoadSettingUIExtensions(builtInSettingExtensions);

            IEnumerable<SettingsUIExtension> externalExtensions = _extContainer.ResolveAll();
            LoadSettingUIExtensions(externalExtensions);
        }

        private List<ISettingsUIComponent> _settingUIExtensions = new List<ISettingsUIComponent>();
        private Dictionary<Type, Type> _configurableExtensionSettingUIExtMapping = new Dictionary<Type, Type>();
        
        private void LoadSettingUIExtensions(IEnumerable<ISettingsUIComponent> extensions)
        {
            foreach (var ext in extensions)
            {
                var configurationRecipient = (IConfigurationRecipient)ext;
                var settingChangeRequestor = (ISettingChangeRequestor)ext;

                _settingUIExtensions.Add(ext);

                if (ext is ISettingsRegistrator settingsRegistrator)
                {
                    foreach (ISettingKey key in settingsRegistrator.GetSettingKeys())
                    {
                        configurationRecipient.PutSetting(key, _settingsService.GetSetting(key));
                    }
                }

                if (ext is ICanRequestAppRestart appRestartRequestor)
                {
                    appRestartRequestor.RestartAppRequested += Ext_AppRestartRequestor_RestartAppRequested;
                }

                ext.SetUpSettingUIParts();
                IReadOnlyDictionary<ISettingKey, ISettingUIPart> extUIParts = ext.GetSettingsViewParts();

                foreach (ISettingKey settingKey in extUIParts.Keys)
                {
                    if (settingKey.TargetComponentType != null 
                        && !_configurableExtensionSettingUIExtMapping.ContainsKey(settingKey.TargetComponentType))
                    {
                        _configurableExtensionSettingUIExtMapping.Add(settingKey.TargetComponentType, ext.GetType());
                    }
                }

                var uiItem = new ExtensionUIItem(extUIParts.Select(d => d.Value).Cast<UIElement>(), ext.GetType());
                switch (ext.GetCategory())
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
                    case SettingsCategory.Advanced:
                        AdvancedExtensionUIItems.Add(uiItem);
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

        private void Ext_AppRestartRequestor_RestartAppRequested(object sender, EventArgs e)
        {
            restartConfirmationPopup.IsOpen = true;
            this.IsEnabled = false;
        }

        private void RestartConfirmationPopup_DialogAnswered(object sender, DialogAnswerEventArgs e)
        {
            this.IsEnabled = true;

            if (e.SelectedButton == DialogButtons.Yes)
            {
                // if we exit with exit code Special, the Guard will restart us.
                Application.Current.Shutdown((int)ProcessExitCodes.Special); 
            }
        }

        private void Ext_ResultPreviewInvalidated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => UpdatePreview((SettingsUIExtension)sender)));
        }

        private void UpdatePreview(ISettingsUIComponent ext)
        {
            PerMonitorDpiWindow wnd = (PerMonitorDpiWindow)Window.GetWindow(this);
            DisplayMonitor monitor = MonitorUtils.GetByWindowHandle(wnd.Handle);

            double maxPreviewHeight = GetThumbnailMaxHeight(monitor, out bool isScaled);
            IPreviewData previewData = ext.GetResultPreviewData(new PreviewParams((int)maxPreviewHeight, (int)maxPreviewHeight, monitor.Dpi, isScaled));

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

        public IEnumerable<Type> GetDependentExtensions(string extensionTypeFullName)
        {
            ISettingsRegistrator settingsRegistrator = _settingUIExtensions
                .FirstOrDefault(ext => ext.GetType().FullName == extensionTypeFullName) as ISettingsRegistrator;

            if (settingsRegistrator != null)
            {
                return settingsRegistrator
                       .GetSettingKeys()
                       .Select(key => key.TargetComponentType);
            }
            else
            {
                return _configurableExtensionSettingUIExtMapping
                    .Where(m => m.Key.FullName == extensionTypeFullName)
                    .Select(m => m.Value);
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

        private AsyncObservableCollection<ExtensionUIItem> AdvancedExtensionUIItems
        {
            get { return (AsyncObservableCollection<ExtensionUIItem>)GetValue(AdvancedExtensionUIItemsProperty); }
            set { SetValue(AdvancedExtensionUIItemsProperty, value); }
        }

        private static readonly DependencyProperty AdvancedExtensionUIItemsProperty =
            DependencyProperty.Register(nameof(AdvancedExtensionUIItems), typeof(AsyncObservableCollection<ExtensionUIItem>), typeof(SettingsWindow), new PropertyMetadata(null));


        /// <summary>
        /// Determines whether to use the vertical item template in the items control
        /// with the setting ui items. If true, the previews are shown inlined (below the setting).
        /// If false, the preview is shown next to the setting (on the right).
        /// </summary>
        public bool UseInlinedPreviews
        {
            get { return (bool)GetValue(UseInlinedPreviewsProperty); }
            set { SetValue(UseInlinedPreviewsProperty, value); }
        }
        
        public static readonly DependencyProperty UseInlinedPreviewsProperty =
            DependencyProperty.Register(nameof(UseInlinedPreviews), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(false));



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

        public bool IsAdvancedTabEnabled
        {
            get { return (bool)GetValue(IsAdvancedTabEnabledProperty); }
            set { SetValue(IsAdvancedTabEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAdvancedTabEnabledProperty =
            DependencyProperty.Register(nameof(IsAdvancedTabEnabled), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(false));


        private readonly ISettingsService _settingsService;

        private readonly SettingsUIExtensionContainer _extContainer;

        private void BtnEnableAdvanced_Click(object sender, RoutedEventArgs e)
        {
            IsAdvancedTabEnabled = true;
        }
    }
}
