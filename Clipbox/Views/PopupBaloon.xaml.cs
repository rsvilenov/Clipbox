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

        public event EventHandler UserConfirmed;

        public PopupBaloon(ISettingsService settingsService)
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            CloseCommand = new DelegateCommand(CloseCommandHandler);

            if (settingsService.GetSetting<bool>(SettingsKeys.AreFadeAnimationsEnabled))
            {
                Opacity = 0;
                Shown += PopupBaloon_Shown;
            }
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
    }
}
