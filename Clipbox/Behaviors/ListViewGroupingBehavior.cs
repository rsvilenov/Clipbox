using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    public class ListViewGroupingBehavior : Behavior<ListView>
    {
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
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
                PropertyGroupDescription groupDescription = new PropertyGroupDescription(GroupByPropertyName);
                view.GroupDescriptions.Add(groupDescription);
            }));
        }
    }
}
