using Clipbox.Infrastructure.UI;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for ScrollBarEx.xaml
    /// </summary>
    public partial class VerticalScrollBarEx : ScrollBar
    {
        private LocalDataContext _localDataContext;

        public VerticalScrollBarEx()
        {
            InitializeComponent();

            _localDataContext = new LocalDataContext();
            DataContext = _localDataContext;
        }

        
        public string TopLabelText
        {
            get { return (string)GetValue(TopLabelTextProperty); }
            set { SetValue(TopLabelTextProperty, value); }
        }

        public static readonly DependencyProperty TopLabelTextProperty =
            DependencyProperty.Register("TopLabelText", typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, TopLabelTextValueChanged));

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
            DependencyProperty.Register("BottomLabelText", typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, BottomLabelTextValueChanged));

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
            DependencyProperty.Register("FloatingLabelText", typeof(string), typeof(VerticalScrollBarEx), new PropertyMetadata(string.Empty, FloatingLabelTextValueChanged));

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
