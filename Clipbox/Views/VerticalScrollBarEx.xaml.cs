using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for ScrollBarEx.xaml
    /// </summary>
    public partial class VerticalScrollBarEx : ScrollBar
    {
        private LocalDataContext _localDataContext;

        private Thumb _thumb;
        private Button _topButton;
        private Button _bottomButton;
        private UIElement _parent;

        private Point? _topButtonRelativeLocation;
        private Point? _bottomButtonRelativeLocation;

        public VerticalScrollBarEx()
        {
            InitializeComponent();

            _localDataContext = new LocalDataContext();
            DataContext = _localDataContext;
            Loaded += VerticalScrollBarEx_Loaded;
        }

        private void VerticalScrollBarEx_Loaded(object sender, RoutedEventArgs e)
        {
            _parent = VisualTreeHelper.GetParent(this) as UIElement;
            _thumb = Traversing.GetVisualChildOfType<Thumb>(this);

            // find the top and bottom buttons
            var scrollButtons = Traversing.GetVisualChildCollection(this).Where(t =>  t is Button).Cast<Button>();
            _topButton = scrollButtons.Where(b => b.Command == ScrollBar.ScrollToTopCommand).Single();
            _bottomButton = scrollButtons.Where(b => b.Command == ScrollBar.ScrollToBottomCommand).Single();
            
            Scroll += VerticalScrollBarEx_Scroll;
        }

        private bool TryLoadAndStoreTopBottomButtonsLocations()
        {
            if (!_topButtonRelativeLocation.HasValue)
            {
                _topButtonRelativeLocation = _topButton.TranslatePoint(new Point(0, 0), _parent);
                if (!_topButtonRelativeLocation.HasValue)
                {
                    return false;
                }
            }

            if (!_bottomButtonRelativeLocation.HasValue)
            {
                _bottomButtonRelativeLocation = _bottomButton.TranslatePoint(new Point(0, 0), _parent);
                if (!_bottomButtonRelativeLocation.HasValue)
                {
                    return false;
                }
            }

            return true;
        }

        private void VerticalScrollBarEx_Scroll(object sender, ScrollEventArgs e)
        {
            Point relativeLocation = _thumb.TranslatePoint(new Point(0, 0), _parent);

            if (!TryLoadAndStoreTopBottomButtonsLocations())
            {
                return;
            }

            if (relativeLocation.Y < _topButtonRelativeLocation.Value.Y + _topButton.ActualHeight)
            {
                _topButton.Visibility = Visibility.Hidden;
            }
            else
            {
                _topButton.Visibility = Visibility.Visible;
            }

            if (relativeLocation.Y + _thumb.ActualHeight > _bottomButtonRelativeLocation.Value.Y)
            {
                _bottomButton.Visibility = Visibility.Hidden;
            }
            else
            {
                _bottomButton.Visibility = Visibility.Visible;
            }
        }

        public string TopLabelText
        {
            get { return (string)GetValue(TopLabelTextProperty); }
            set { SetValue(TopLabelTextProperty, value); }
        }

        public static readonly DependencyProperty TopLabelTextProperty =
            DependencyProperty.Register(nameof(TopLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, TopLabelTextValueChanged));

        private static void TopLabelTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VerticalScrollBarEx)._localDataContext.TopLabelText = e.NewValue as string;
        }

        public string BottomLabelText
        {
            get { return (string)GetValue(BottomLabelTextProperty); }
            set { SetValue(BottomLabelTextProperty, value); }
        }
        
        public static readonly DependencyProperty BottomLabelTextProperty =
            DependencyProperty.Register(nameof(BottomLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, BottomLabelTextValueChanged));

        private static void BottomLabelTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VerticalScrollBarEx)._localDataContext.BottomLabelText = e.NewValue as string;
        }


        public string FloatingLabelText
        {
            get { return (string)GetValue(FloatingLabelTextProperty); }
            set { SetValue(FloatingLabelTextProperty, value); }
        }
        
        public static readonly DependencyProperty FloatingLabelTextProperty =
            DependencyProperty.Register(nameof(FloatingLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, FloatingLabelTextValueChanged));

        private static void FloatingLabelTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VerticalScrollBarEx)._localDataContext.FloatingLabelText = e.NewValue as string;
        }

        private class LocalDataContext : BindableObject
        {
            private string _topLabelText;
            public string TopLabelText
            {
                get { return _topLabelText; }
                set { SetField(ref _topLabelText, value); }
            }

            private string _bottomLabelText;
            public string BottomLabelText
            {
                get { return _bottomLabelText; }
                set { SetField(ref _bottomLabelText, value); }
            }

            private string _floatingLabelText;
            public string FloatingLabelText
            {
                get { return _floatingLabelText; }
                set { SetField(ref _floatingLabelText, value); }
            }

        }

    }
}
