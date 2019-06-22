using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Utils
{
    /// <summary>
    /// Utils, which use VisualTreeHelper and LogicalTreeHelper to traverse the ui element trees
    /// </summary>
    public static class Traversing
    {
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }


        
        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            // https://stackoverflow.com/questions/14875042/finding-all-child-controls-wpf
            List<T> logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }

        public static List<UIElement> GetLogicalChildCollection(UIElement parent)
        {
            List<UIElement> logicalCollection = new List<UIElement>();
            GetLogicalChildCollection(parent as UIElement, logicalCollection);
            return logicalCollection.Distinct().ToList();
        }

        private static void GetLogicalChildCollection(UIElement parent, List<UIElement> logicalCollection)
        {
            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (var child in children)
            {
                if (child is UIElement)
                {
                    UIElement uiChild = child as UIElement;

                    logicalCollection.Add(uiChild);
                    GetLogicalChildCollection(uiChild, logicalCollection);
                }
            }
        }

        public static List<UIElement> GetVisualChildCollection(this UIElement parent)
        {
            return GetVisualChildCollection(parent, (p) => true);
        }

        public static List<UIElement> GetVisualChildCollection(this UIElement parent, Func<UIElement, bool> matchCondition, bool collectMatchChildren = true)
        {
            List<UIElement> visualChildCollection = new List<UIElement>();
            GetVisualChildCollection(parent as UIElement, visualChildCollection, matchCondition, collectMatchChildren);
            return visualChildCollection.Distinct().ToList();
        }

        private static void GetVisualChildCollection(UIElement parent, List<UIElement> visualChildlCollection, Func<UIElement, bool> matchCondition, bool collectMatchChildren = true)
        {
            if (parent is FrameworkElement)
                (parent as FrameworkElement).ApplyTemplate();

            // TODO: revise this

            // collect the children of the ListView
            if (parent is ListView)
            {
                var listView = parent as ListView;
                ItemContainerGenerator generator = listView.ItemContainerGenerator;
                for (int i = 0; i < generator.Items.Count; i++)
                {
                    ListBoxItem selectedItem = (ListBoxItem)generator.ContainerFromIndex(i);
                    if (matchCondition(selectedItem))
                    {
                        visualChildlCollection.Add(selectedItem);
                    }
                }
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is UIElement)
                {
                    UIElement uiChild = child as UIElement;
                    if (matchCondition(uiChild))
                    {
                        visualChildlCollection.Add(uiChild);
                        // if we set this flag, every child which also meets the condition will also be included in the return collection.
                        // if not, only the top element matching the condition will be returned.
                        if (collectMatchChildren)
                        {
                            GetVisualChildCollection(uiChild, visualChildlCollection, matchCondition, true);
                        }
                    }
                    else
                    {
                        GetVisualChildCollection(uiChild, visualChildlCollection, matchCondition, collectMatchChildren);
                    }
                }
            }

        }

        public static T GetVisualChildOfType<T>(this DependencyObject depObj)
            where T : DependencyObject
        {
            // https://stackoverflow.com/questions/10279092/how-to-get-children-of-a-wpf-container-by-type
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetVisualChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
