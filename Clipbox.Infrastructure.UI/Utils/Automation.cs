using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class Automation
    {
        /// <summary>
        /// Raises the Preview* mouse events
        /// </summary>
        /// <param name="window">The window to be used as the event source</param>
        /// <param name="child">The element for which the events will be raised</param>
        /// <returns>'True' if the event is handled.</returns>
        private static bool RaiseTunnelingEvents(Window window, UIElement child)
        {
            // https://stackoverflow.com/questions/3033839/firing-mouseleftbuttondown-event-programmatically
            int timestamp = new TimeSpan(DateTime.Now.Ticks).Milliseconds;
            const MouseButton mouseButton = MouseButton.Left;
            
            var prevMouseDownArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp, mouseButton)
            {
                RoutedEvent = UIElement.PreviewMouseDownEvent,
                Source = window
            };


            child.RaiseEvent(prevMouseDownArgs);
            if (prevMouseDownArgs.Handled)
                return true; // stop on the first event handler, which handles the event

            var prevLeftButtonDownArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp, mouseButton)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                Source = window
            };

            child.RaiseEvent(prevLeftButtonDownArgs);
            if (prevLeftButtonDownArgs.Handled)
                return true;

            return false;
        }

        /// <summary>
        /// Raise the bubbling mouse events
        /// </summary>
        /// <param name="window">The window to be used as the event source</param>
        /// <param name="child">The element for which the events will be raised</param>
        private static void RaiseBubblingEvents(Window window, UIElement child)
        {
            int timestamp = new TimeSpan(DateTime.Now.Ticks).Milliseconds;
            const MouseButton mouseButton = MouseButton.Left;
            
            var mouseDownEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp, mouseButton)
            {
                RoutedEvent = UIElement.MouseDownEvent,
                Source = window
            };
            
            child.RaiseEvent(mouseDownEventArgs);

            var mouseLeftButtonDownEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, timestamp, mouseButton)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = window
            };
            
            child.RaiseEvent(mouseLeftButtonDownEventArgs);
        }

        /// <summary>
        /// Toggles the selection of the ListViewItem
        /// </summary>
        /// <param name="lvi"></param>
        private static void ToggleSelectionOfListViewItem(ListViewItem lvi)
        {
            lvi.IsSelected = !lvi.IsSelected;

            // do not execute any click events on the ListViewItem, 
            // as each of them will trigger the SelectionChanged event of the ListView,
            // causing this event to fire multiple times
        }

        /// <summary>
        /// Raise the direct Click event, execute the associated command and
        /// if the buttin is ToggleButton, toggle its checked state
        /// </summary>
        /// <param name="child"></param>
        private static void DoButtonBaseClickOperations(ButtonBase btn)
        {
            btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            if (btn.Command != null && btn.Command.CanExecute(btn.CommandParameter))
            {
                btn.Command.Execute(btn.CommandParameter);
            }

            if (btn is ToggleButton)
            {
                var tog = btn as ToggleButton;
                tog.IsChecked = !tog.IsChecked;
            }
        }

        /// <summary>
        /// Perform the operations for mouse click without actual clicking.
        /// </summary>
        /// <param name="containerWindow"></param>
        public static void DoClickOperationsUnderMouse(Window containerWindow)
        {
            IInputElement directlyOverElement = Mouse.DirectlyOver;
            if (!(directlyOverElement is UIElement))
            {
                return;
            }

            UIElement uiElement = (UIElement)directlyOverElement;

            bool eventHandled = RaiseTunnelingEvents(containerWindow, uiElement);
            if (eventHandled)
            {
                return;
            }

            if (uiElement is ButtonBase)
            {
                RaiseBubblingEvents(containerWindow, uiElement);
                DoButtonBaseClickOperations(uiElement as ButtonBase);
            }
            else if (uiElement is ListViewItem)
            {
                // do not raise the click events for these items, as this may cause the item to be selected and then unselected
                ToggleSelectionOfListViewItem(uiElement as ListViewItem);
            }
            else
            {
                var buttonBaseParent = Traversing.FindParent<ButtonBase>(uiElement);
                if (buttonBaseParent != null)
                {
                    RaiseBubblingEvents(containerWindow, buttonBaseParent);
                    DoButtonBaseClickOperations(buttonBaseParent);
                }
                else
                {
                    var lviParent = Traversing.FindParent<ListViewItem>(uiElement);
                    if (lviParent != null)
                    {
                        ToggleSelectionOfListViewItem(lviParent);
                    }
                }
            }
        }
    }
}
