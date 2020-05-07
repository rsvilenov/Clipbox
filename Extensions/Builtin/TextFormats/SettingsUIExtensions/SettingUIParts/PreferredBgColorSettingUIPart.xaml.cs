using Clipbox.Infrastructure.UI.ColorPicker;
using Clipbox.Infrastructure.UI.ColorPicker.EventArgsClasses;
using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for CompactifySettingUIPart.xaml
    /// </summary>
    public partial class PreferredBgColorSettingUIPart : SettingUIPart<string>
    {
        private ColorPickerDialog _colorPickerDialog;
        private Popup _colorPickerDialogPopup;

        private string _hexColorString;
        
        public PreferredBgColorSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            Loaded += PreferredBgColorSettingUIPart_Loaded;
        }

        private void PreferredBgColorSettingUIPart_Loaded(object sender, RoutedEventArgs e)
        {
            SetColorBrushes(GetSettingValue());
        }

        
        public bool IsForegroundSelector
        {
            get { return (bool)GetValue(IsForegroundSelectorProperty); }
            set { SetValue(IsForegroundSelectorProperty, value); }
        }
        
        public static readonly DependencyProperty IsForegroundSelectorProperty =
            DependencyProperty.Register(nameof(IsForegroundSelector), typeof(bool), typeof(PreferredBgColorSettingUIPart), new PropertyMetadata(false));


        public SolidColorBrush SelectedColorBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedColorBrushProperty); }
            set { SetValue(SelectedColorBrushProperty, value); }
        }
        
        public static readonly DependencyProperty SelectedColorBrushProperty =
            DependencyProperty.Register(nameof(SelectedColorBrush), typeof(SolidColorBrush), typeof(PreferredBgColorSettingUIPart), new PropertyMetadata(Brushes.White));

        private void SetColorBrushes(string hexColStr)
        {
            var brushConverter = new BrushConverter();
            try
            {
                SelectedColorBrush = (SolidColorBrush)(brushConverter.ConvertFrom(hexColStr));
                ContrastForeground = new SolidColorBrush(ColorPicker.GetContrastColor(SelectedColorBrush.Color));
            }
            catch
            {
                SelectedColorBrush = Brushes.White;
                ContrastForeground = Brushes.Black;
            }
            
        }



        public SolidColorBrush ContrastForeground
        {
            get { return (SolidColorBrush)GetValue(ContrastForegroundProperty); }
            set { SetValue(ContrastForegroundProperty, value); }
        }

        public static readonly DependencyProperty ContrastForegroundProperty =
            DependencyProperty.Register(nameof(ContrastForeground), typeof(SolidColorBrush), typeof(PreferredBgColorSettingUIPart), new PropertyMetadata(Brushes.White));



        public override string GetSettingValue()
        {
            return _hexColorString;
        }

        public override void SetSettingValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (IsForegroundSelector)
                {
                    // the default foreground
                    _hexColorString = Colors.Black.ToString();
                }
                else
                {
                    // the default background
                    _hexColorString = Colors.Gainsboro.ToString();
                }
            }
            else
            {
                _hexColorString = value;
            }

            SetColorBrushes(_hexColorString);
        }
        
        
        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_colorPickerDialogPopup == null)
            {
                _colorPickerDialog = new ColorPickerDialog(SelectedColorBrush?.Color ?? Colors.Green);
                _colorPickerDialog.ColorSelected += _colorPickerDialog_ColorSelected;

                _colorPickerDialogPopup = new Popup
                {
                    MinWidth = 400,
                    Child = _colorPickerDialog,
                    PlacementTarget = Window.GetWindow(this),
                    Placement = PlacementMode.Center,
                    IsOpen = true
                };
            }
        }

        private void _colorPickerDialog_ColorSelected(object sender, ColorEventArgs e)
        {
            if (e.Color.HasValue)
            {
                _hexColorString = e.Color.ToString();
                SetColorBrushes(_hexColorString);
                NotifySettingModified();
            }

            _colorPickerDialogPopup.IsOpen = false;
            _colorPickerDialog.ColorSelected -= _colorPickerDialog_ColorSelected;
            _colorPickerDialogPopup = null;
            _colorPickerDialog = null;
        }

        private void RevertToDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsForegroundSelector)
            {
                // the default foreground
                _hexColorString = Colors.Black.ToString();
            }
            else
            {
                // the default background
                _hexColorString = Colors.Gainsboro.ToString();
            }

            SetColorBrushes(_hexColorString);
            NotifySettingModified();
        }
    }
}
