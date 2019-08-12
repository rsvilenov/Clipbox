using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Visuals
{
    /// <summary>
    /// This panel is developed for the purpose of the QuickView and is meant to look like the 
    /// Alt+Tab switcher of MS Windows
    /// </summary>
    public class AutoWrapPanel : Panel
    {

        const double DEFAULT_ITEM_MIN_WIDTH = 20;
        const double DEFAULT_ITEM_MIN_HEIGHT = 20;

        public double ItemMinWidth
        {
            get { return (double)GetValue(ItemMinWidthProperty); }
            set { SetValue(ItemMinWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemMinWidthProperty =
            DependencyProperty.Register("ItemMinWidth", typeof(double), typeof(AutoWrapPanel), new PropertyMetadata(DEFAULT_ITEM_MIN_WIDTH));



        public double ItemMinHeight
        {
            get { return (double)GetValue(ItemMinHeightProperty); }
            set { SetValue(ItemMinHeightProperty, value); }
        }

        public static readonly DependencyProperty ItemMinHeightProperty =
            DependencyProperty.Register("ItemMinHeight", typeof(double), typeof(AutoWrapPanel), new PropertyMetadata(DEFAULT_ITEM_MIN_HEIGHT));
        
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // The panel's MeasureOverride is not guaranteed to be called on every resize... so we need to force this.
            InvalidateMeasure();
            base.OnRenderSizeChanged(sizeInfo);
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            double maxLineWidth = 0.0;
            double totalHeight = 0.0;
            Size size = new Size(0, 0);

            // Measure the children, so that we can use DesiredSize later
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);
            }


            double maxWidth = this.MaxWidth;

            // If MaxWidth has not been specified, try to guess it
            if (double.IsPositiveInfinity(this.MaxWidth))
            {
                maxWidth = GetMaxWidthFromParentWithMaxWidth();
            }

            // get the max size
            GetLines(maxWidth, out maxLineWidth, out totalHeight);

            size.Width = double.IsPositiveInfinity(availableSize.Width) ? maxLineWidth : availableSize.Width;
            size.Height = double.IsPositiveInfinity(availableSize.Height) ? totalHeight : availableSize.Height;
            
            return size;
        }

        private double GetMaxWidthFromParentWithMaxWidth()
        {
            // find the first parent with MaxWidth
            FrameworkElement parentWithMaxWidth = null;
            DependencyObject parentDepObject = this;
            do
            {
                parentDepObject = VisualTreeHelper.GetParent(parentDepObject);
                FrameworkElement parentFrameworkElement = parentDepObject as FrameworkElement;
                if (parentFrameworkElement != null && !double.IsPositiveInfinity(parentFrameworkElement.MaxWidth) &&
                    !(parentFrameworkElement is ItemsPresenter) && !(parentFrameworkElement is ScrollContentPresenter))
                {
                    parentWithMaxWidth = parentFrameworkElement;
                    break;
                }
            }
            while (parentDepObject != null);

            // if such a parent has been found, get its MaxWidth and exclude the space between the parent and our element.
            if (parentWithMaxWidth != null)
            {
                Thickness calculatedMargin = CalculateActualMargin(parentWithMaxWidth, this);
                return parentWithMaxWidth.MaxWidth - (calculatedMargin.Left + calculatedMargin.Right);
            }
            else
            {
                Debug.WriteLine("WARNING: No parent with MaxWidth was found. Please ensure that the value has been set first or that the binding has been evaluated.");
            }

            return double.PositiveInfinity;
        }

        public Thickness CalculateActualMargin(UIElement parent, UIElement child)
        {
            var scrollViewer = Traversing.FindParent<ScrollViewer>(child);
            if (scrollViewer != null)
            {
                child = scrollViewer;
            }
            Point relativeLeftTopLocation = child.TranslatePoint(new Point(0, 0), parent);

            return new Thickness(
                /* Left */
                relativeLeftTopLocation.X,
                /* Top */
                relativeLeftTopLocation.Y,
                /* Right */
                parent.DesiredSize.Width - relativeLeftTopLocation.X - child.DesiredSize.Width,
                /* Bottom */
                parent.DesiredSize.Height - relativeLeftTopLocation.Y - child.DesiredSize.Height
                );
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            double maxLineWidth = 0.0;
            double totalHeight = 0.0;
            var lines = GetLines(finalSize.Width, out maxLineWidth, out totalHeight);
            // arrange the elements in each line
            double yPosition = 0.0;

            foreach (int line in lines.Values.Distinct())
            {
                var childrenForLine = lines.Where(l => l.Value == line).Select(l => l.Key);
                double totalLineLength = childrenForLine.Sum(uielement => uielement.DesiredSize.Width);
                double xPosition = (finalSize.Width - totalLineLength) / 2;
                if (xPosition < 0)
                {
                    throw new InvalidOperationException("Can't determine the proper X position of the current line.");
                }

                foreach (UIElement child in childrenForLine)
                {
                    Rect rect = new Rect(new Point(xPosition, yPosition), child.DesiredSize);
                    
                    xPosition += child.DesiredSize.Width;
                    child.Arrange(rect);
                }

                yPosition += childrenForLine.Max(c => c.DesiredSize.Height);
            }

            return finalSize;
        }

        // TODO: refactor this method
        private Dictionary<UIElement, int> GetLines(double containerWidth, out double maxLineWidth, out double totalHeight)
        {
            var lines = new Dictionary<UIElement, int>();
            maxLineWidth = 0.0;
            totalHeight = 0.0;
            
            // divide the elements into lines
            int lineNumber = 0;
            double totalLineWidth = 0.0;
            double maxLineHeight = 0.0;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                totalLineWidth = totalLineWidth + child.DesiredSize.Width;

                if (child.DesiredSize.Height > maxLineHeight)
                {
                    maxLineHeight = child.DesiredSize.Height;
                }

                if (i == 0 && totalLineWidth > containerWidth)
                {
                    return lines;
                    //throw new InvalidCastException("The child is bigger than the container. Check panel's measure phase.");
                }

                if (totalLineWidth > containerWidth)
                {
                    // Debug.WriteLine($"ActualWidth: {ActualWidth} TotalLineWidth: {totalLineWidth}");
                    lineNumber++;
                    totalLineWidth = child.DesiredSize.Width;
                    //totalHeight += maxLineHeight;
                    maxLineHeight = 0.0;
                }


                if (totalLineWidth > maxLineWidth)
                {
                    maxLineWidth = totalLineWidth;
                }


                lines.Add(child, lineNumber);
            }

            if (lines.Any())
            {
                for (int ln = 0; ln < lineNumber + 1; ln++)
                {
                    totalHeight += lines.Where(l => l.Value == ln).Max(l => l.Key.DesiredSize.Height);
                }
            }

            return lines;
        }
    }
}
