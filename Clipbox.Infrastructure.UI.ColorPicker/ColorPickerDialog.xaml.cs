using Clipbox.Infrastructure.UI.ColorPicker.EventArgsClasses;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        public ColorPickerDialog()
            : this(Colors.Green)
        { }

        public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;

            _initialColor = initialColor;

            colorPicker.SelectedColorChanged += ColorPicker_SelectedColorChanged;
            colorPicker.PickerInitialized += ColorPicker_PickerInitialized;

            OkClicked = new DelegateCommand(OnOkButton);
            CancelClicked = new DelegateCommand(OnCancelButton);
        }

        private void ColorPicker_PickerInitialized(object sender, EventArgs e)
        {
            colorPicker.SetSelectedColor(_initialColor);
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
        }
        
    }
}
