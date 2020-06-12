using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Win32;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_Caption", Type = typeof(FrameworkElement))]
    public class WindowFrame : AcryllicWindow
    {
        static WindowFrame()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowFrame), new FrameworkPropertyMetadata(typeof(WindowFrame)));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            MonitorUtils.DisplayMonitor dm = MonitorUtils.GetByWindowHandle(this.Handle);

            double scaleFactor = dm.Dpi / 96;
            double currentDisplayHeight = dm.WorkingArea.Height * scaleFactor;
            double currentDisplayWidth = dm.WorkingArea.Width * scaleFactor;

            this.MaxHeight = currentDisplayHeight;
            this.MaxWidth = currentDisplayWidth;

            base.OnSourceInitialized(e);
        }


        public bool IsCloseButtonVisible
        {
            get { return (bool)GetValue(IsCloseButtonVisibleProperty); }
            set { SetValue(IsCloseButtonVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsCloseButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsCloseButtonVisible), typeof(bool), typeof(WindowFrame), new PropertyMetadata(true));



        public bool IsMinimizeButtonVisible
        {
            get { return (bool)GetValue(IsMinimizeButtonVisibleProperty); }
            set { SetValue(IsMinimizeButtonVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsMinimizeButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsMinimizeButtonVisible), typeof(bool), typeof(WindowFrame), new PropertyMetadata(true));


        public string CloseBtnToolTip
        {
            get { return (string)GetValue(CloseBtnToolTipProperty); }
            set { SetValue(CloseBtnToolTipProperty, value); }
        }

        public static readonly DependencyProperty CloseBtnToolTipProperty =
            DependencyProperty.Register(nameof(CloseBtnToolTip), typeof(string), typeof(WindowFrame), new PropertyMetadata(string.Empty));


        public string MinimizeBtnToolTip
        {
            get { return (string)GetValue(MinimizeBtnToolTipProperty); }
            set { SetValue(MinimizeBtnToolTipProperty, value); }
        }

        public static readonly DependencyProperty MinimizeBtnToolTipProperty =
            DependencyProperty.Register(nameof(MinimizeBtnToolTip), typeof(string), typeof(WindowFrame), new PropertyMetadata(string.Empty));


        public string MaximizeBtnToolTip
        {
            get { return (string)GetValue(MaximizeBtnToolTipProperty); }
            set { SetValue(MaximizeBtnToolTipProperty, value); }
        }

        public static readonly DependencyProperty MaximizeBtnToolTipProperty =
            DependencyProperty.Register(nameof(MaximizeBtnToolTip), typeof(string), typeof(WindowFrame), new PropertyMetadata(string.Empty));


        public string RestoreBtnToolTip
        {
            get { return (string)GetValue(RestoreBtnToolTipProperty); }
            set { SetValue(RestoreBtnToolTipProperty, value); }
        }

        public static readonly DependencyProperty RestoreBtnToolTipProperty =
            DependencyProperty.Register(nameof(RestoreBtnToolTip), typeof(string), typeof(WindowFrame), new PropertyMetadata(string.Empty));




        public Brush CaptionForeground
        {
            get { return (Brush)GetValue(CaptionForegroundProperty); }
            set { SetValue(CaptionForegroundProperty, value); }
        }

        public static readonly DependencyProperty CaptionForegroundProperty =
            DependencyProperty.Register(nameof(CaptionForeground), typeof(Brush), typeof(WindowFrame), new PropertyMetadata(default(Brush)));



        public Brush CaptionBackground
        {
            get { return (Brush)GetValue(CaptionBackgroundProperty); }
            set { SetValue(CaptionBackgroundProperty, value); }
        }

        public static readonly DependencyProperty CaptionBackgroundProperty =
            DependencyProperty.Register(nameof(CaptionBackground), typeof(Brush), typeof(WindowFrame), new PropertyMetadata(Brushes.Transparent));



        public override void OnApplyTemplate()
        {
            if (!(this.Template.FindName("PART_Caption", this) is FrameworkElement container))
            {
                throw new InvalidOperationException("PART_Caption is not present in the template or it is not a FrameworkElement.");
            }
            
            container.MouseMove += Container_MouseMove;
            container.PreviewMouseLeftButtonDown += Container_PreviewMouseLeftButtonDown;
            
            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            MaximizeWindow();
        }

        private void MaximizeWindow()
        {
            // since we have WindowStyle="None", the system command for resizing will not work
            // and we have to resize the window manually.
            WindowState = WindowState.Maximized;

            MonitorUtils.DisplayMonitor dm = MonitorUtils.GetByWindowHandle(this.Handle);
            WindowUtils.SetWindowsPosition(this.Handle, dm.WorkingArea.Left,
                dm.WorkingArea.Top,
                dm.WorkingArea.Width,
                dm.WorkingArea.Height);
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void Container_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                e.Handled = true;

                if (WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(this);
                }
                else if (WindowState == WindowState.Normal)
                {
                    MaximizeWindow();
                }
            }
        }

        private void Container_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);

                EventHandler stateChangedEventHandler = null;

                stateChangedEventHandler = new EventHandler
                    ((_, args) =>
                    {
                        System.Drawing.Rectangle windowRect = WindowUtils.GetWindowRectangle(this.Handle);

                        MonitorUtils.DisplayMonitor dm = MonitorUtils.GetByWindowHandle(this.Handle);

                        WindowUtils.SetWindowsPosition(this.Handle,
                            dm.WorkingArea.Left,
                            dm.WorkingArea.Top,
                            windowRect.Width,
                            windowRect.Height);

                        InitiateDragMove();

                        this.StateChanged -= stateChangedEventHandler;
                    });

                this.StateChanged += stateChangedEventHandler;
            }
            else
            {
                InitiateDragMove();
            }
        }

        private void InitiateDragMove()
        {
            // check if the mouse button is still down
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch (InvalidOperationException)
                {
                    // If the button is not down anymore,
                    // DragMove will throw an InvalidOperationException
                }
            }
        }

    }
}
