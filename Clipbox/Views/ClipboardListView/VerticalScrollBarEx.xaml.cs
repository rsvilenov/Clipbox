using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Clipbox.Views.ClipboardListView
{
    /// <summary>
    /// Interaction logic for ScrollBarEx.xaml
    /// </summary>
    public partial class VerticalScrollBarEx : ScrollBar
    {
        private Thumb _thumb;
        private Button _topButton;
        private Button _bottomButton;
        private UIElement _parent;

        private Point? _topButtonRelativeLocation;
        private Point? _bottomButtonRelativeLocation;

        private double _lastThumbTopPosition;

        public VerticalScrollBarEx()
        {
            InitializeComponent();
            
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
            
            // 'Scrolled' fires only when the thumb is dragged directly and is not affected by the mouse wheel
            // ... so I attempted to use 'ValueChanged' instead,
            // but when the content size changes and the thumb moves up/down as a consequence,
            // the thumb label doesn't get updated because both these events do not fire.
            // So I ended up using LayoutUpdated.
            LayoutUpdated += VerticalScrollBarEx_LayoutUpdated;
        }

        private void VerticalScrollBarEx_LayoutUpdated(object sender, System.EventArgs e)
        {
            if (_thumb != null && _thumb.IsLoaded)
            {
                Point thumbRelativeLocation = _thumb.TranslatePoint(new Point(0, 0), this);
                if (_lastThumbTopPosition != thumbRelativeLocation.Y)
                {
                    UpdateButtonsVisibility();
                    _lastThumbTopPosition = thumbRelativeLocation.Y;
                }
            }
        }

        private bool TryLoadAndStoreTopBottomButtonsLocations(bool forceLoad = false)
        {
            if (_topButton == null 
                || !_topButton.IsLoaded
                || _bottomButton == null
                || !_bottomButton.IsLoaded)
            {
                return false;
            }

            if (!_topButtonRelativeLocation.HasValue || forceLoad)
            {
                _topButtonRelativeLocation = _topButton.TranslatePoint(new Point(0, 0), _parent);
                if (!_topButtonRelativeLocation.HasValue)
                {
                    return false;
                }
            }

            if (!_bottomButtonRelativeLocation.HasValue || forceLoad)
            {
                _bottomButtonRelativeLocation = _bottomButton.TranslatePoint(new Point(0, 0), _parent);
                if (!_bottomButtonRelativeLocation.HasValue)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateButtonsVisibility()
        {
            if (_parent == null)
            {
                return;
            }

            Point relativeThumbLocation = _thumb.TranslatePoint(new Point(0, 0), _parent);

            if (!TryLoadAndStoreTopBottomButtonsLocations())
            {
                return;
            }

            if (relativeThumbLocation.Y < _topButtonRelativeLocation.Value.Y + _topButton.ActualHeight)
            {
                _topButton.Visibility = Visibility.Hidden;
            }
            else
            {
                _topButton.Visibility = Visibility.Visible;
            }

            if (relativeThumbLocation.Y + _thumb.ActualHeight > _bottomButtonRelativeLocation.Value.Y)
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
            DependencyProperty.Register(nameof(TopLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty));
        
        public string BottomLabelText
        {
            get { return (string)GetValue(BottomLabelTextProperty); }
            set { SetValue(BottomLabelTextProperty, value); }
        }
        
        public static readonly DependencyProperty BottomLabelTextProperty =
            DependencyProperty.Register(nameof(BottomLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, BottomLabelTextValueChanged));

        private static void BottomLabelTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var _this = (VerticalScrollBarEx)d;
                _this.TryLoadAndStoreTopBottomButtonsLocations(forceLoad: true);
                _this.UpdateButtonsVisibility();
            }
        }


        public string FloatingLabelText
        {
            get { return (string)GetValue(FloatingLabelTextProperty); }
            set { SetValue(FloatingLabelTextProperty, value); }
        }
        
        public static readonly DependencyProperty FloatingLabelTextProperty =
            DependencyProperty.Register(nameof(FloatingLabelText), typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty));
        
    }
}
