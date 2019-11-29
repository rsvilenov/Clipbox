using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    public class ListViewGroupingBehavior : Behavior<ListView>
    {
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ListViewGroupingBehavior), new PropertyMetadata(true));

        
        public string GroupByPropertyName
        {
            get { return (string)GetValue(GroupByPropertyNameProperty); }
            set { SetValue(GroupByPropertyNameProperty, value); }
        }
        
        public static readonly DependencyProperty GroupByPropertyNameProperty =
            DependencyProperty.Register(nameof(GroupByPropertyName), typeof(string), typeof(ListViewGroupingBehavior), new PropertyMetadata(string.Empty));
        
        protected override void OnAttached()
        {
            AssociatedObject.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => {
                if (IsEnabled)
                {
                    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
                    PropertyGroupDescription groupDescription = new PropertyGroupDescription(GroupByPropertyName);
                    view.GroupDescriptions.Add(groupDescription);
                }
            }));
        }
    }
}
