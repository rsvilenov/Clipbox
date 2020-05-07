using Clipbox.Infrastructure.UI.ColorPicker.EventArgsClasses;
using Clipbox.Infrastructure.UI.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.ColorPicker
{
    /// <summary>
    /// Interaction logic for ColorPickerDialog.xaml
    /// </summary>
    public partial class ColorPickerDialog : UserControl
    {
        public event EventHandler<ColorEventArgs> ColorSelected;

        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        private Color _initialColor;
        private Color? _selectedColor;
        private PerMonitorDpiWindow _pmDpiWindow;

        public ColorPickerDialog()
            : this(Colors.Green)
        { }

        public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;

            _pmDpiWindow = Window.GetWindow(this) as PerMonitorDpiWindow;
            if (_pmDpiWindow != null)
            {
                _pmDpiWindow.DPIChanged += PmDpiWnd_DPIChanged;
            }

            _initialColor = initialColor;

            colorPicker.SelectedColorChanged += ColorPicker_SelectedColorChanged;
            colorPicker.PickerInitialized += ColorPicker_PickerInitialized;

            OkClicked = new DelegateCommand(OnOkButton);
            CancelClicked = new DelegateCommand(OnCancelButton);
            Unloaded += ColorPickerDialog_Unloaded;
        }

        private void ColorPickerDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_pmDpiWindow != null)
            {
                _pmDpiWindow.DPIChanged -= PmDpiWnd_DPIChanged;
            }
        }

        private void PmDpiWnd_DPIChanged(object sender, DpiChangedEventArgs e)
        {
            loadingOverlay.Visibility = Visibility.Visible;
            colorPicker.ReloadPalette();
        }

        private void ColorPicker_PickerInitialized(object sender, EventArgs e)
        {
            colorPicker.SetSelectedColor(_initialColor);
            if (Window.GetWindow(this) is PerMonitorDpiWindow pmDpiWnd)
            {
                pmDpiWnd.DPIChanged += PmDpiWnd_DPIChanged;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => loadingOverlay.Visibility = Visibility.Collapsed));
        }

        private void OnCancelButton()
        {
            ColorSelected?.Invoke(this, new ColorEventArgs(null));
        }

        private void OnOkButton()
        {
            ColorSelected?.Invoke(this, new ColorEventArgs(_selectedColor));
        }

        private void ColorPicker_SelectedColorChanged(object sender, EventArgs e)
        {
            _selectedColor = colorPicker.SelectedColor;
            searchTextBox.Text = colorPicker.SelectedColorHexStr;
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            Color typedColor;
            try
            {
                typedColor = (Color)ColorConverter.ConvertFromString(searchTextBox.Text);
                colorPicker.SetSelectedColor(typedColor);
            }
            catch { }
        }
    }
}
