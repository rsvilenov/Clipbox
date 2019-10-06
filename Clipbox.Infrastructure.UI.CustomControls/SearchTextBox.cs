using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_MainBorder", Type = typeof(Border))]
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_TextBlockOverlay", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ClearButton", Type = typeof(Button))]
    public class SearchTextBox : Control
    {
        private TextBox _textBox;
        private TextBlock _textBlockOverlay;
        private Button _clearButton;

        private SlidingTimer _searchSlidingTimer;


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchTextBox), new PropertyMetadata(string.Empty, Text_PropertyChangedy));

        private static void Text_PropertyChangedy(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((SearchTextBox)d);
            if (self._textBox != null)
            {
                self._textBox.Text = (string)e.NewValue;
                self.SetControlsVisibility();
            }
        }

        public string OverlayText
        {
            get { return (string)GetValue(OverlayTextProperty); }
            set { SetValue(OverlayTextProperty, value); }
        }

        public static readonly DependencyProperty OverlayTextProperty =
            DependencyProperty.Register(nameof(OverlayText), typeof(string), typeof(SearchTextBox), new PropertyMetadata(string.Empty, OverlayText_PropertyChangedy));

        private static void OverlayText_PropertyChangedy(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((SearchTextBox)d);
            if (self._textBlockOverlay != null)
            {
                self._textBlockOverlay.Text = (string)e.NewValue;
            }
        }

        public int SearchOnInputIdleDelayMS
        {
            get { return (int)GetValue(SearchOnInputIdleDelayMSProperty); }
            set { SetValue(SearchOnInputIdleDelayMSProperty, value); }
        }

        public static readonly DependencyProperty SearchOnInputIdleDelayMSProperty =
            DependencyProperty.Register(nameof(SearchOnInputIdleDelayMS), typeof(int), typeof(SearchTextBox), new PropertyMetadata(1000, SearchOnInputIdleDelayMS_PropertyChanged));

        private static void SearchOnInputIdleDelayMS_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SearchTextBox)d)._searchSlidingTimer.IntervalMs = (int)e.NewValue;
        }

        public static readonly ICommand SearchRequestedCommand = new RoutedUICommand(nameof(SearchRequestedCommand), nameof(SearchRequestedCommand),
                                                        typeof(SearchTextBox),
                                                        new InputGestureCollection(
                                                            new InputGesture[] {
                                                                new KeyGesture(Key.Enter),
                                                                new MouseGesture(MouseAction.LeftClick) }
                                                                )
                                                          );

        public static readonly RoutedEvent SearchRequestedEvent = EventManager.RegisterRoutedEvent(nameof(SearchRequested),
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchTextBox));

        public event RoutedEventHandler SearchRequested
        {
            add { AddHandler(SearchRequestedEvent, value); }
            remove { RemoveHandler(SearchRequestedEvent, value); }
        }

        private void OnSearchRequested()
        {
            RoutedEventArgs args = new RoutedEventArgs(SearchRequestedEvent);
            RaiseEvent(args);
        }

        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(typeof(SearchTextBox)));

            CommandManager.RegisterClassCommandBinding(typeof(SearchTextBox),
                new CommandBinding(SearchTextBox.SearchRequestedCommand, null));
        }

        public SearchTextBox()
        {
            IsVisibleChanged += SearchTextBox_IsVisibleChanged;
            GotFocus += SearchTextBox_GotFocus;
            PreviewMouseDown += SearchTextBox_PreviewMouseDown;
        }

        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // if the click occurs on the hidden clear button or anywhere else inside the control,
            // focus the text box. If the click is on the button, let it handle the click.
            HitTestResult r = VisualTreeHelper.HitTest(this, e.GetPosition(this));

            if ((r.VisualHit.GetType() == typeof(Button) && _clearButton != null && !_clearButton.IsVisible)
                || r.VisualHit.GetType() != typeof(Button))
            {
                _textBox.Focus();
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox != null)
            {
                _textBox.Focus();
            }
        }

        private void SearchTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                _searchSlidingTimer = new SlidingTimer
                {
                    IntervalMs = SearchOnInputIdleDelayMS
                };
                _searchSlidingTimer.Elapsed += _searchSlidingTimer_Elapsed;
            }
            else
            {
                if (_searchSlidingTimer != null)
                {
                    _searchSlidingTimer.Elapsed -= _searchSlidingTimer_Elapsed;
                    _searchSlidingTimer.Dispose();
                }
            }
        }

        private void _searchSlidingTimer_Elapsed(object sender, System.EventArgs e)
        {
            if (SearchRequestedCommand.CanExecute(Text))
            {
                SearchRequestedCommand.Execute(Text);
            }

            OnSearchRequested();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.Template != null)
            {
                WireUpTextBoxOverlay();

                WireUpTextBox();

                WireUpClearButton();
            }
        }
        
        private void WireUpTextBoxOverlay()
        {
            TextBlock textBlockOverlay = this.Template.FindName("PART_TextBlockOverlay", this) as TextBlock;
            if (textBlockOverlay != _textBlockOverlay)
            {
                _textBlockOverlay = textBlockOverlay;
                
                if (_textBlockOverlay != null)
                {
                    // make the label click-through
                    _textBlockOverlay.IsHitTestVisible = false;
                    _textBlockOverlay.Text = OverlayText;
                }

            }
        }

        private void WireUpTextBox()
        {
            TextBox textBox = this.Template.FindName("PART_TextBox", this) as TextBox;
            if (textBox != _textBox)
            {
                if (_textBox != null)
                {
                    _textBox.KeyUp -= _textBox_KeyUp;
                }

                _textBox = textBox;

                if (_textBox != null)
                {
                    _textBox.Text = Text;
                    _textBox.KeyUp += _textBox_KeyUp;
                }
            }
        }

        private void WireUpClearButton()
        {
            Button clearButton = this.Template.FindName("PART_ClearButton", this) as Button;
            if (clearButton != _clearButton)
            {
                if (_clearButton != null)
                {
                    _clearButton.Click -= _clearButton_Click;
                }

                _clearButton = clearButton;

                if (_clearButton != null)
                {
                    _clearButton.Visibility = Visibility.Hidden;
                    _clearButton.Click += _clearButton_Click;
                }
            }
        }

        private void _clearButton_Click(object sender, RoutedEventArgs e)
        {
            _textBox.Clear();
            Text = string.Empty;
            _textBlockOverlay.Visibility = Visibility.Visible;
            _clearButton.Visibility = Visibility.Hidden;
            OnSearchRequested();
        }

        private void _textBox_KeyUp(object sender, KeyEventArgs e)
        {
            SetControlsVisibility();

            Text = _textBox.Text;
            _searchSlidingTimer.Slide();
        }

        private void SetControlsVisibility()
        {
            if (_textBox.Text?.Length > 0)
            {
                _textBlockOverlay.Visibility = Visibility.Collapsed;
                _clearButton.Visibility = Visibility.Visible;
            }
            else
            {
                _clearButton.Visibility = Visibility.Hidden;
                _textBlockOverlay.Visibility = Visibility.Visible;
            }
        }
    }
}
