using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Threading;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// Change the tooltip according to the IsChecked status of the ToggleButton.
    /// Doing this with triggers creates all sorts of timing issues, so the
    /// behavior approach turns out to be better.
    /// </summary>
    public class ToggleButtonToolTipBehavior : Behavior<ToggleButton>
    {
        private ToolTip _toolTip;

        public string CheckedToolTipText
        {
            get { return (string)GetValue(CheckedToolTipTextProperty); }
            set { SetValue(CheckedToolTipTextProperty, value); }
        }
        
        public static readonly DependencyProperty CheckedToolTipTextProperty =
            DependencyProperty.Register(nameof(CheckedToolTipText), typeof(string), typeof(ToggleButtonToolTipBehavior), new PropertyMetadata(string.Empty));
        
        public string UncheckedToolTipText
        {
            get { return (string)GetValue(UncheckedToolTipTextProperty); }
            set { SetValue(UncheckedToolTipTextProperty, value); }
        }
        
        public static readonly DependencyProperty UncheckedToolTipTextProperty =
            DependencyProperty.Register(nameof(UncheckedToolTipText), typeof(string), typeof(ToggleButtonToolTipBehavior), new PropertyMetadata(string.Empty));
        
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // try to get the tooltip
            _toolTip = ToolTipService.GetToolTip(AssociatedObject) as ToolTip;

            // if there is no tooltip specified for this togglebutton, initialize one
            if (_toolTip == null)
            {
                _toolTip = new ToolTip
                {
                    Content = AssociatedObject.IsChecked == true ? CheckedToolTipText : UncheckedToolTipText
                };

                AssociatedObject.ToolTip = _toolTip;
            }

            _toolTip.IsVisibleChanged += _toolTip_IsVisibleChanged;

            AssociatedObject.Checked += AssociatedObject_Checked;
            AssociatedObject.Unchecked += AssociatedObject_Unchecked;
        }

        private void _toolTip_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
                if (AssociatedObject.IsChecked == true)
                {
                    _toolTip.Content = CheckedToolTipText;
                }
                else
                {
                    _toolTip.Content = UncheckedToolTipText;
                }
            }
        }

        private void AssociatedObject_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => _toolTip.IsOpen = false));
        }

        private void AssociatedObject_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => _toolTip.IsOpen = false));
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            _toolTip.IsVisibleChanged -= _toolTip_IsVisibleChanged;

            AssociatedObject.Checked -= AssociatedObject_Checked;
            AssociatedObject.Unchecked -= AssociatedObject_Unchecked;
        }
    }
}
