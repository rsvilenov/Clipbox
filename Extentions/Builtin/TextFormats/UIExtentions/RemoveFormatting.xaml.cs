using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Extentions.BuiltIn.TextFormats.UIExtentions
{
    /// <summary>
    /// Interaction logic for RemoveFormatting.xaml
    /// </summary>
    public partial class RemoveFormatting : UserControl
    {
        internal event EventHandler<RemoveFormattingEventArgs> RemoveFormattingChanged;

        public RemoveFormatting()
        {
            InitializeComponent();
        }
        
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleRemoveFormatting(true);
            RemoveFormattingChanged?.Invoke(this, new RemoveFormattingEventArgs(true));
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleRemoveFormatting(false);
            RemoveFormattingChanged?.Invoke(this, new RemoveFormattingEventArgs(false));
        }


        internal void ToggleRemoveFormatting(bool toggled)
        {
            TgRemoveFormatting.IsChecked = toggled;
        }

        internal class RemoveFormattingEventArgs : EventArgs
        {
            internal RemoveFormattingEventArgs(bool toggled)
            {
                Toggled = toggled;
            }

            internal bool Toggled { get; private set; }
        }

    }
}
