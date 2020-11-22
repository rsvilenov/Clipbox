using Clipbox.Infrastructure.UI;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for PopupHint.xaml
    /// </summary>
    public partial class PopupHint : Popup
    {
        public ICommand CloseCommand { get; set; }

        public PopupHint()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            CloseCommand = new DelegateCommand(CloseCommandHandler);
        }

        private void CloseCommandHandler()
        {
            IsOpen = false;
        }

        public bool AreFadeAnimationsEnabled
        {
            get { return (bool)GetValue(AreFadeAnimationsEnabledProperty); }
            set { SetValue(AreFadeAnimationsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreFadeAnimationsEnabledProperty =
            DependencyProperty.Register(nameof(AreFadeAnimationsEnabled), typeof(bool), typeof(PopupHint), new PropertyMetadata(true));



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PopupHint), new PropertyMetadata(string.Empty));



    }
}
