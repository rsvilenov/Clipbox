using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ColorPicker;
using Clipbox.Infrastructure.UI.ColorPicker.EventArgsClasses;
using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for CompactifySettingUIPart.xaml
    /// </summary>
    public partial class PreferredColorSettingUIPart : SettingUIPart<string>
    {
        public ICommand RevertToDefault { get; set; }
        public ICommand SelectCustomColor { get; set; }

        private ColorPickerDialog _colorPickerDialog;
        private Popup _colorPickerDialogPopup;

        private string _hexColorString;

        private readonly string DEFAULT_FG_COLOR = Colors.Black.ToString();
        private readonly string DEFAULT_BG_COLOR = Colors.Gainsboro.ToString();


        public PreferredColorSettingUIPart(bool isForegroundSelector)
            : this()
        {
            IsForegroundSelector = isForegroundSelector;
            LoadRecommendedColors();
            SelectedRecommendedColor = IsForegroundSelector ? DEFAULT_FG_COLOR : DEFAULT_BG_COLOR;
        }


        public PreferredColorSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            RevertToDefault = new DelegateCommand(RevertToDefaultCommandInvoked, RevertToDefaultCommandCanExecute);
            SelectCustomColor = new DelegateCommand(SelectCustomColorCommandInvoked);

            if (RecommendedColors == null)
            {
                LoadRecommendedColors();
            }
        }

        
        private void LoadRecommendedColors()
        {
            RecommendedColors = new AsyncObservableCollection<string>();

            if (IsForegroundSelector)
            {
                RecommendedColors.Add(Colors.Black.ToString());
                RecommendedColors.Add("#7e735f");
                RecommendedColors.Add("#007acc");
                RecommendedColors.Add("#a3a3a3");
                RecommendedColors.Add(Colors.White.ToString());
            }
            else
            {
                RecommendedColors.Add(Colors.White.ToString());
                RecommendedColors.Add(Colors.Gainsboro.ToString());
                RecommendedColors.Add("#7e7d7d");
                RecommendedColors.Add("#2a455b");
                RecommendedColors.Add("#1e1d1c");
            }
        }

        private bool IsForegroundSelector
        {
            get { return (bool)GetValue(IsForegroundSelectorProperty); }
            set { SetValue(IsForegroundSelectorProperty, value); }
        }

        private static readonly DependencyProperty IsForegroundSelectorProperty =
            DependencyProperty.Register(nameof(IsForegroundSelector), typeof(bool), typeof(PreferredColorSettingUIPart), new PropertyMetadata(false));


        private SolidColorBrush SelectedCustomColorBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedCustomColorBrushProperty); }
            set { SetValue(SelectedCustomColorBrushProperty, value); }
        }
        
        private static readonly DependencyProperty SelectedCustomColorBrushProperty =
            DependencyProperty.Register(nameof(SelectedCustomColorBrush), typeof(SolidColorBrush), typeof(PreferredColorSettingUIPart), new PropertyMetadata(Brushes.White));
        

        private AsyncObservableCollection<string> RecommendedColors
        {
            get { return (AsyncObservableCollection<string>)GetValue(RecommendedColorsProperty); }
            set { SetValue(RecommendedColorsProperty, value); }
        }

        private static readonly DependencyProperty RecommendedColorsProperty =
            DependencyProperty.Register(nameof(RecommendedColors), typeof(AsyncObservableCollection<string>), typeof(PreferredColorSettingUIPart), new PropertyMetadata(null));




        private string SelectedRecommendedColor
        {
            get { return (string)GetValue(SelectedRecommendedColorProperty); }
            set { SetValue(SelectedRecommendedColorProperty, value); }
        }

        private static readonly DependencyProperty SelectedRecommendedColorProperty =
            DependencyProperty.Register(nameof(SelectedRecommendedColor), typeof(string), typeof(PreferredColorSettingUIPart), new PropertyMetadata(null, SelectedRecommendedColor_PropertyChanged));

        private static void SelectedRecommendedColor_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string newColor && !string.IsNullOrEmpty(newColor))
            {
                var self = ((PreferredColorSettingUIPart)d);
                self.IsCustomColorSelected = false;
                self._hexColorString = newColor;
                self.NotifySettingModified();
            }
        }

        private SolidColorBrush ContrastForeground
        {
            get { return (SolidColorBrush)GetValue(ContrastForegroundProperty); }
            set { SetValue(ContrastForegroundProperty, value); }
        }

        private static readonly DependencyProperty ContrastForegroundProperty =
            DependencyProperty.Register(nameof(ContrastForeground), typeof(SolidColorBrush), typeof(PreferredColorSettingUIPart), new PropertyMetadata(Brushes.White));




        public bool IsCustomColorChosen
        {
            get { return (bool)GetValue(IsCustomColorChosenProperty); }
            set { SetValue(IsCustomColorChosenProperty, value); }
        }
        
        public static readonly DependencyProperty IsCustomColorChosenProperty =
            DependencyProperty.Register(nameof(IsCustomColorChosen), typeof(bool), typeof(PreferredColorSettingUIPart), new PropertyMetadata(false));



        public bool IsCustomColorSelected
        {
            get { return (bool)GetValue(IsCustomColorSelectedProperty); }
            set { SetValue(IsCustomColorSelectedProperty, value); }
        }
        
        public static readonly DependencyProperty IsCustomColorSelectedProperty =
            DependencyProperty.Register(nameof(IsCustomColorSelected), typeof(bool), typeof(PreferredColorSettingUIPart), new PropertyMetadata(false));



        public override string GetSettingValue()
        {
            return _hexColorString;
        }

        public override void SetSettingValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _hexColorString = IsForegroundSelector ? DEFAULT_FG_COLOR : DEFAULT_BG_COLOR;
            }
            else
            {
                _hexColorString = value;
            }

            if (!RecommendedColors.Contains(_hexColorString))
            {
                IsCustomColorChosen = true;
                IsCustomColorSelected = true;
                SelectedRecommendedColor = null;
            }
            else
            {
                SelectedRecommendedColor = _hexColorString;
            }
        }

        private void _colorPickerDialog_ColorSelected(object sender, ColorEventArgs e)
        {
            if (e.Color.HasValue)
            {
                _hexColorString = e.Color.ToString();
                SelectedCustomColorBrush = new SolidColorBrush(e.Color.Value);

                if (!RecommendedColors.Contains(_hexColorString))
                {
                    IsCustomColorSelected = true;
                    IsCustomColorChosen = true;
                    SelectedRecommendedColor = null;
                }
                else
                {
                    SelectedRecommendedColor = _hexColorString;
                }

                NotifySettingModified();
            }

            _colorPickerDialogPopup.IsOpen = false;
            _colorPickerDialog.ColorSelected -= _colorPickerDialog_ColorSelected;
            _colorPickerDialogPopup = null;
            _colorPickerDialog = null;
        }
        

        private void CustomColorPreviewContentControl_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsCustomColorSelected = true;
            SelectedRecommendedColor = null;
            _hexColorString = SelectedCustomColorBrush.Color.ToString();
            NotifySettingModified();
        }

        private void SelectCustomColorCommandInvoked()
        {
            if (_colorPickerDialogPopup == null)
            {
                _colorPickerDialog = new ColorPickerDialog(SelectedCustomColorBrush?.Color ?? Colors.Green);
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

        private bool RevertToDefaultCommandCanExecute()
        {
            return _hexColorString != (IsForegroundSelector ? DEFAULT_FG_COLOR : DEFAULT_BG_COLOR);
        }

        private void RevertToDefaultCommandInvoked()
        {
            _hexColorString = IsForegroundSelector ? DEFAULT_FG_COLOR : DEFAULT_BG_COLOR;
            if (!RecommendedColors.Contains(_hexColorString))
            {
                throw new InvalidOperationException($"The default color should be present in {nameof(RecommendedColors)} collection.");
            }

            SelectedRecommendedColor = _hexColorString;

            NotifySettingModified();
        }
    }
}
