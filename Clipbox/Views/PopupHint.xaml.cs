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
            if (AnchorElement != null)
            {
                AnchorElement.LayoutUpdated += AnchorElement_LayoutUpdated;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            AnchorElement.LayoutUpdated -= AnchorElement_LayoutUpdated;
        }

        private void AnchorElement_LayoutUpdated(object sender, EventArgs e)
        {
            AdjustPosition();
        }

        private void CloseCommandHandler()
        {
            IsOpen = false;
        }

        private void AdjustPosition()
        {
            if (AnchorElement != null && AnchorElement.IsLoaded)
            {
                Placement = PlacementMode.Absolute;
                Window parentWindow = Window.GetWindow(AnchorElement);
                if (parentWindow != null && parentWindow.IsVisible)
                {
                    Point relativePoint;

                    try
                    {
                        relativePoint = AnchorElement.TransformToAncestor(parentWindow)
                                          .Transform(new Point(0, 0));
                    }
                    catch (InvalidOperationException) // the visual tree is not ready yet
                    {
                        return;
                    }

                    HorizontalOffset = relativePoint.X + (AnchorElement.ActualWidth / 2);
                        VerticalOffset = relativePoint.Y + AnchorElement.ActualHeight;
                   
                }
            }
        }


        public FrameworkElement AnchorElement
        {
            get { return (FrameworkElement)GetValue(AnchorElementProperty); }
            set { SetValue(AnchorElementProperty, value); }
        }

        public static readonly DependencyProperty AnchorElementProperty =
            DependencyProperty.Register(nameof(AnchorElement), typeof(FrameworkElement), typeof(PopupHint), new PropertyMetadata(null, AnchorElement_PropertyChanged));

        private static void AnchorElement_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PopupHint)d;
            if (self.AnchorElement != null)
            {
                self.LayoutUpdated += self.AnchorElement_LayoutUpdated;
            }
            self.AdjustPosition();
        }



        public bool IsTopPointing
        {
            get { return (bool)GetValue(IsTopPointingProperty); }
            set { SetValue(IsTopPointingProperty, value); }
        }

        public static readonly DependencyProperty IsTopPointingProperty =
            DependencyProperty.Register(nameof(IsTopPointing), typeof(bool), typeof(PopupHint), new PropertyMetadata(true));


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
