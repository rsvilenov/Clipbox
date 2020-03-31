using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for LoadingIndicatorPopup.xaml
    /// </summary>
    public partial class LoadingIndicatorPopup : Window
    {
        public static int FADE_OUT_ANIMATION_DURATION_MS = 500;
        public static int FADE_IN_ANIMATION_DURATION_MS = FADE_OUT_ANIMATION_DURATION_MS * 3;

        public LoadingIndicatorPopup()
        {
            InitializeComponent();
            
            LoadingIndicator.Opacity = 0;
        }

        public IntPtr Handle { get; private set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
            // Hide the window from Alt+Tab
            WindowUtils.AddWindowStyleEx(Handle, NativeEnums.WindowStylesEx.WS_EX_TOOLWINDOW);

            base.OnSourceInitialized(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            DoubleAnimation anim = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromMilliseconds(FADE_IN_ANIMATION_DURATION_MS));
            LoadingIndicator.BeginAnimation(UIElement.OpacityProperty, anim);
        }
        
        public void FadeOut()
        {
            DoubleAnimation anim = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(FADE_OUT_ANIMATION_DURATION_MS));

            // Handling the Completed event doesn't work reliably, 
            // since for some reason the animation does not always start.
            // TODO: figure out why
            // anim.Completed += (s, _) => this.Close();

            Task.Delay(FADE_OUT_ANIMATION_DURATION_MS)
                .ContinueWith((t) => Dispatcher.Invoke(() => this.Close()));

            LoadingIndicator.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
