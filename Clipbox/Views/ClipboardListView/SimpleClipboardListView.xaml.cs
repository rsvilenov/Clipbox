using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardListView
{
    /// <summary>
    /// Interaction logic for SimpleClipboardListView.xaml
    /// </summary>
    public partial class SimpleClipboardListView : ClipboardListViewBase
    {
        public event EventHandler<EventArgs> UncombinableItemsSelected;

        /// <summary>
        /// Place a semi-transparent veil over the item when it is not focused
        /// </summary>
        public bool DimNonFocusedItems
        {
            get { return (bool)GetValue(DimNonFocusedItemsProperty); }
            set { SetValue(DimNonFocusedItemsProperty, value); }
        }

        public static readonly DependencyProperty DimNonFocusedItemsProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItems), typeof(bool), typeof(SimpleClipboardListView), new PropertyMetadata(false));




        public Button ScrollUpButton
        {
            get { return (Button)GetValue(ScrollUpButtonProperty); }
            set { SetValue(ScrollUpButtonProperty, value); }
        }

        public static readonly DependencyProperty ScrollUpButtonProperty =
            DependencyProperty.Register(nameof(ScrollUpButton), typeof(Button), typeof(SimpleClipboardListView), new PropertyMetadata(null));



        public Button ScrollDownButton
        {
            get { return (Button)GetValue(ScrollDownButtonProperty); }
            set { SetValue(ScrollDownButtonProperty, value); }
        }

        public static readonly DependencyProperty ScrollDownButtonProperty =
            DependencyProperty.Register(nameof(ScrollDownButton), typeof(Button), typeof(SimpleClipboardListView), new PropertyMetadata(null));
        
        public SimpleClipboardListView()
        {
            InitializeComponent();
        }

        public void FocusNextItem()
        {
            // changing the selected index will not cause SelectionChanged to fire, but assigning it for the first time will fire the event
            if (this.SelectedIndex == -1)
            {
                this.SelectedIndex = 0;
            }
            else
            {
                if (this.SelectedIndex == this.Items.Count - 1)
                {
                    this.SelectedIndex = 0;
                }
                else
                {
                    this.SelectedIndex++;
                }

                base.PerformOnSelectedChangedOpreations();
            }
        }

        public override void ResumeDefaultState()
        {
            this.UnselectAll();
            UnselectMultiItemSubItems();
            SelectedItemIds.Clear();
            SelectedMultiItemsIds.Clear();
        }

        protected override void OnSelectedItemsCombinabilityChanged(SelectedItemsCombinabilityArgs args)
        {
            if (!args.AreItemsCombinable)
            {
                UncombinableItemsSelected?.Invoke(this, new EventArgs());
                args.CancelLastSelection = true;
            }
        }
    }
}
