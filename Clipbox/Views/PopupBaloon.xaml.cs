using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for PopupBaloon.xaml
    /// </summary>
    public partial class PopupBaloon : AcryllicWindow
    {
        private const int FADE_IN_DURATION_MS = 1000;

        public ICommand CloseCommand { get; set; }
        public ICommand CustomActionCommand { get; set; }

        public event EventHandler UserConfirmed;
        public event EventHandler CustomActionRequested;

        public PopupBaloon(ISettingsService settingsService)
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            CloseCommand = new DelegateCommand(CloseCommandHandler);
            CustomActionCommand = new DelegateCommand(CustomActionCommandHandler);

            if (settingsService.GetSetting<bool>(SettingsKeys.AreFadeAnimationsEnabled))
            {
                Opacity = 0;
                Shown += PopupBaloon_Shown;
            }
        }

        private void CustomActionCommandHandler()
        {
            CustomActionRequested?.Invoke(this, new EventArgs());
            this.Close();
        }

        private void CloseCommandHandler()
        {
            UserConfirmed?.Invoke(this, new EventArgs());
            this.Close();
        }

        private void PopupBaloon_Shown(object sender, EventArgs e)
        {
            Shown -= PopupBaloon_Shown;

            DoubleAnimation animation = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(FADE_IN_DURATION_MS)));
            this.BeginAnimation(Window.OpacityProperty, animation);
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PopupBaloon), new PropertyMetadata(string.Empty));


        public string CustomActionButtonText
        {
            get { return (string)GetValue(CustomActionButtonTextProperty); }
            set { SetValue(CustomActionButtonTextProperty, value); }
        }

        public static readonly DependencyProperty CustomActionButtonTextProperty =
            DependencyProperty.Register(nameof(CustomActionButtonText), typeof(string), typeof(PopupBaloon), new PropertyMetadata(string.Empty));

        public string CustomActionDismissButtonText
        {
            get { return (string)GetValue(CustomActionDismissButtonTextProperty); }
            set { SetValue(CustomActionDismissButtonTextProperty, value); }
        }

        public static readonly DependencyProperty CustomActionDismissButtonTextProperty =
            DependencyProperty.Register(nameof(CustomActionDismissButtonText), typeof(string), typeof(PopupBaloon), new PropertyMetadata(string.Empty));

    }
}
