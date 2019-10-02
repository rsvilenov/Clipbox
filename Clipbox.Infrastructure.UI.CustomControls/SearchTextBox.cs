using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_MainBorder", Type = typeof(Border))]
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_TextBoxOverlay", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ClearButton", Type = typeof(Button))]
    public class SearchTextBox : Control
    {
        private TextBox _textBox;
        private TextBox _textBoxOverlay;
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
            if (self._textBoxOverlay != null)
            {
                self._textBoxOverlay.Text = (string)e.NewValue;
            }
        }

        public int SearchOnInputIdleDelayMS
        {
            get { return (int)GetValue(SearchOnInputIdleDelayMSProperty); }
            set { SetValue(SearchOnInputIdleDelayMSProperty, value); }
        }

        public static readonly DependencyProperty SearchOnInputIdleDelayMSProperty =
            DependencyProperty.Register(nameof(SearchOnInputIdleDelayMS), typeof(int), typeof(SearchTextBox), new PropertyMetadata(500, SearchOnInputIdleDelayMS_PropertyChanged));

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
            TextBox textBoxOverlay = this.Template.FindName("PART_TextBoxOverlay", this) as TextBox;
            if (textBoxOverlay != _textBoxOverlay)
            {
                if (_textBoxOverlay != null)
                {
                    _textBoxOverlay.GotFocus -= _textBoxOverlay_GotFocus;
                }

                _textBoxOverlay = textBoxOverlay;

                if (_textBoxOverlay != null)
                {
                    _textBoxOverlay.Text = OverlayText;
                    _textBoxOverlay.GotFocus += _textBoxOverlay_GotFocus;
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
            _textBoxOverlay.Visibility = Visibility.Visible;
            _clearButton.Visibility = Visibility.Hidden;
            OnSearchRequested();
        }

        private void _textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (_textBox.Text?.Length > 0)
            {
                Text = _textBox.Text;
                _clearButton.Visibility = Visibility.Visible;
                _searchSlidingTimer.Slide();
            }
            else
            {
                _clearButton.Visibility = Visibility.Hidden;
            }
        }
        

        private void _textBoxOverlay_GotFocus(object sender, RoutedEventArgs e)
        {
            _textBoxOverlay.Visibility = Visibility.Collapsed;
            _textBox.Focus();
        }
    }
}
