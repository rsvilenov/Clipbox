using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions
{
    /// <summary>
    /// Interaction logic for MultipleTextSelectionsAlignment.xaml
    /// </summary>
    public partial class MultipleTextSelectionsAlignment : UserControl
    {
        internal event EventHandler<SeparatorEventArgs> SeparatorChanged;

        public MultipleTextSelectionsAlignment()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        internal void SetSeparatorType(TextConcatenateSeparators separatorType)
        {
            switch(separatorType)
            {
                case TextConcatenateSeparators.None:
                    TgNewLine.IsChecked = false;
                    TgSpace.IsChecked = false;
                    TgNoSeparator.IsChecked = true;
                    break;
                case TextConcatenateSeparators.Space:
                    TgNoSeparator.IsChecked = false;
                    TgNewLine.IsChecked = false;
                    TgSpace.IsChecked = true;
                    break;
                case TextConcatenateSeparators.NewLine:
                    TgSpace.IsChecked = false;
                    TgNoSeparator.IsChecked = false;
                    TgNewLine.IsChecked = true;
                    break;

            }
            
            SeparatorChanged?.Invoke(this, new SeparatorEventArgs(separatorType));
        }

        private void ToggleButtonNoSeparator_Checked(object sender, RoutedEventArgs e)
        {
            SetSeparatorType(TextConcatenateSeparators.None);
        }

        private void ToggleButtonSpace_Checked(object sender, RoutedEventArgs e)
        {
            SetSeparatorType(TextConcatenateSeparators.Space);
        }
        
        private void ToggleButtonNewLine_Checked(object sender, RoutedEventArgs e)
        {
            SetSeparatorType(TextConcatenateSeparators.NewLine);
        }

        internal class SeparatorEventArgs : EventArgs
        {
            internal SeparatorEventArgs(TextConcatenateSeparators separator)
            {
                Separator = separator;
            }

            internal TextConcatenateSeparators Separator { get; private set; }
        }

        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelsProperty =
            DependencyProperty.Register(nameof(ShowLabels), typeof(bool), typeof(MultipleTextSelectionsAlignment), new PropertyMetadata(false));


    }
}
