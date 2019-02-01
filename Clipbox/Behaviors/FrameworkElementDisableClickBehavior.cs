using System;
using System.Windows;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    public class FrameworkElementDisableClickBehavior : Behavior<FrameworkElement>
    {
        private FrameworkElement _ancestorElement;

        private bool _isWiredUp;

        public FrameworkElement AncestorElement
        {
            get { return (FrameworkElement)GetValue(AncestorElementProperty); }
            set { SetValue(AncestorElementProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Element.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AncestorElementProperty =
            DependencyProperty.Register("AncestorElement", typeof(FrameworkElement), typeof(FrameworkElementDisableClickBehavior), new PropertyMetadata(AncestorElementChanged));

        private static void AncestorElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (FrameworkElementDisableClickBehavior)d;
            if (instance._isWiredUp)
            {
                instance.Unwire();
            }

            instance._ancestorElement = instance.AncestorElement;
            instance.WireUp();
        }

        protected override void OnAttached()
        {
            if (!_isWiredUp)
            {
                WireUp();
            }
        }

        private void WireUp()
        {
            if (_ancestorElement != null)
            {
                if (!AssociatedObject.IsDescendantOf(_ancestorElement))
                {
                    // Do this to avoid unexpected behavior, i.e. a memory leak when the _element gets unloaded
                    // and could not be GCd because AssociatedObject is still alive and its behavior collection 
                    // contains this behavior, which holds a reference to _element.
                    throw new InvalidOperationException("'Element' should be up the visual tree of AssociatedObject.");
                }
                _ancestorElement.PreviewMouseDown += PreviewMouseDownHandler;
            }
            else
            {
                AssociatedObject.PreviewMouseDown += PreviewMouseDownHandler;
            }

            _isWiredUp = true;
        }
        
        protected override void OnDetaching()
        {
            if (_isWiredUp)
            {
                Unwire();
            }
        }

        private void Unwire()
        {
            if (_ancestorElement != null)
            {
                _ancestorElement.PreviewMouseDown -= PreviewMouseDownHandler;
            }
            else
            {
                AssociatedObject.PreviewMouseDown -= PreviewMouseDownHandler;
            }

            _isWiredUp = false;
        }

        private void PreviewMouseDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
