using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Diagnostics;
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
        public ICommand ConfirmCommand { get; set; }
        public ICommand DeclineCommand { get; set; }

        public event EventHandler<PopupHintResponseEventArgs> ClosedByUser;

        private bool _isClosed;
        private bool _areAnchorElementEventsWired;

        private static double _initialDpi;
        private static bool _hasDpiChanged;

        public PopupHint()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            CloseCommand = new DelegateCommand(CloseCommandHandler);
            ConfirmCommand = new DelegateCommand(ConfirmCommandHandler);
            DeclineCommand = new DelegateCommand(DeclineCommandHandler);
            Loaded += PopupHint_Loaded;
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return null;
        }

        private void PopupHint_Loaded(object sender, RoutedEventArgs e)
        {
            if (AnchorElement != null)
            {
                AdjustPosition();
                AnchorElement.LayoutUpdated += AnchorElement_LayoutUpdated;
                AnchorElement.IsVisibleChanged += AnchorElement_IsVisibleChanged;
                _areAnchorElementEventsWired = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_areAnchorElementEventsWired)
            {
                _areAnchorElementEventsWired = false;
                AnchorElement.LayoutUpdated -= AnchorElement_LayoutUpdated;
                AnchorElement.IsVisibleChanged -= AnchorElement_IsVisibleChanged;
            }

            // this is very important! without calling it, the Closed event will NOT be fired
            base.OnClosed(e);
        }

        private void AnchorElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                Close();
            }
        }

        private void AnchorElement_LayoutUpdated(object sender, EventArgs e)
        {
            AdjustPosition();
        }

        private void CloseCommandHandler()
        {
            Close();
            ClosedByUser?.Invoke(this, new PopupHintResponseEventArgs(PopupHintResponseEventArgs.PopupHintResponse.Close));
        }


        private void DeclineCommandHandler()
        {
            Close();
            ClosedByUser?.Invoke(this, new PopupHintResponseEventArgs(PopupHintResponseEventArgs.PopupHintResponse.Decline));
        }

        private void ConfirmCommandHandler()
        {
            Close();
            ClosedByUser?.Invoke(this, new PopupHintResponseEventArgs(PopupHintResponseEventArgs.PopupHintResponse.Confirm));
        }

        public void Open()
        {
            IsOpen = true;
            _isClosed = false;
        }

        public void Close()
        {
            _isClosed = true;
            IsOpen = false;
        }

        private void AdjustPosition()
        {
            if (!_isClosed && AnchorElement != null && AnchorElement.IsLoaded)
            {
                Placement = PlacementMode.Absolute;
                Window parentWindow = Window.GetWindow(AnchorElement);
                if (parentWindow != null && parentWindow.IsVisible)
                {
                    if (parentWindow is PerMonitorDpiWindow dpiWindow)
                    {
                        if (_initialDpi == 0)
                        {
                            _initialDpi = dpiWindow.CurrentDPI;
                        }
                        else if (_hasDpiChanged || _initialDpi != dpiWindow.CurrentDPI)
                        {
                            // popups do not position correctly when the dpi changes,
                            // regardless of the fact that each new popup is a newly created instance...
                            _hasDpiChanged = true;
                            Debug.WriteLine("The dpi of the monitor has changed and the hint cannot be shown, as it does not handle dpi changes well.");
                            IsOpen = false;
                            return;
                        }
                    }

                    Point relativePoint;

                    Point topArrowPointerPoint;

                    try
                    {
                        relativePoint = AnchorElement
                            .TransformToAncestor(parentWindow)
                            .Transform(new Point(0, 0));

                        topArrowPointerPoint = topArrow
                            .TransformToAncestor(this.Child)
                            .Transform(new Point(0, 0));
                    }
                    catch (InvalidOperationException) // the visual tree is not ready yet
                    {
                        return;
                    }

                    switch (ArrowDirection)
                    {
                        case PopupArrowDirection.None:
                            HorizontalOffset = relativePoint.X + AnchorElement.ActualWidth + 15;
                            VerticalOffset = relativePoint.Y + (AnchorElement.ActualHeight / 2);
                            break;
                        case PopupArrowDirection.Left:
                            HorizontalOffset = relativePoint.X + AnchorElement.ActualWidth;
                            VerticalOffset = relativePoint.Y + (AnchorElement.ActualHeight / 2);
                            break;
                        case PopupArrowDirection.Top:
                            HorizontalOffset = relativePoint.X + (AnchorElement.ActualWidth / 2) - (topArrowPointerPoint.X + topArrow.ActualWidth / 2);
                            VerticalOffset = relativePoint.Y + AnchorElement.ActualHeight;
                            break;
                    }

                    // if the popup falls outside of the parent window, hide it
                    if (HorizontalOffset + ActualWidth > parentWindow.ActualWidth ||
                        VerticalOffset + ActualHeight > parentWindow.ActualHeight)
                    {
                        Debug.WriteLine("The hint cannot show, because if falls outside of its parent window");
                        IsOpen = false;
                        return;
                    }

                    IsOpen = true;
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


        public PopupArrowDirection ArrowDirection
        {
            get { return (PopupArrowDirection)GetValue(ArrowDirectionProperty); }
            set { SetValue(ArrowDirectionProperty, value); }
        }

        public static readonly DependencyProperty ArrowDirectionProperty =
            DependencyProperty.Register(nameof(ArrowDirection), typeof(PopupArrowDirection), typeof(PopupHint), new PropertyMetadata(PopupArrowDirection.Top));


        public bool IsQuestion
        {
            get { return (bool)GetValue(IsQuestionProperty); }
            set { SetValue(IsQuestionProperty, value); }
        }

        public static readonly DependencyProperty IsQuestionProperty =
            DependencyProperty.Register(nameof(IsQuestion), typeof(bool), typeof(PopupHint), new PropertyMetadata(false));
    }

    public enum PopupArrowDirection
    {
        None,
        Top,
        Left
    }
}
