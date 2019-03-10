using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.ViewModels.BindableObjects;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for MainViewClipboardContents.xaml
    /// </summary>
    public partial class MainViewClipboardContents : ClipboardViewControl
    {
        private MainClipboardViewViewModel _viewModel;

        public MainViewClipboardContents(MainClipboardViewViewModel vm, UIExtentionContainer container, ISettingsService settingsService)
            : base (container, settingsService)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel;
            IsVisibleChanged += MainViewClipboardContents_IsVisibleChanged;

            MainListView.LayoutUpdated += MainListView_LayoutUpdated;
            
        }

        private void MainListView_LayoutUpdated(object sender, EventArgs e)
        {

            LoadItemsAndGroupsCachedInfo(MainListView);
        }

        private void MainViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel.LoadItems();
        }


        private int _lastListViewItemCount = 0;
        private Dictionary<string, Tuple<ListViewItem, ListViewItem>> _groupBoundingLvis = new Dictionary<string, Tuple<ListViewItem, ListViewItem>>();
        
        private void PART_VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            // find the listview control
            ListView listView = Traversing.FindParent<ListView>(scrollBar);
            // find the thumb and its location
            Thumb thumb = Traversing.GetVisualChildOfType<Thumb>(scrollBar);
            Point relativeThumbPoint = thumb.TranslatePoint(new Point(0, 0), scrollBar);
            double thumbYPos = relativeThumbPoint.Y;

            foreach (string groupName in _groupBoundingLvis.Keys)
            {
                Tuple<ListViewItem, ListViewItem> lvis = _groupBoundingLvis[groupName];

                Point firstLviRelativeLocation = lvis.Item1.TranslatePoint(new Point(0, 0), listView);

                Point lastLviRelativeLocation = firstLviRelativeLocation;
                double lastItemHeight = lvis.Item1.ActualHeight;
                if (lvis.Item2 != null)
                {
                    lastLviRelativeLocation = lvis.Item2.TranslatePoint(new Point(0, 0), listView);
                    lastItemHeight = lvis.Item2.ActualHeight;
                }

                if (thumbYPos >= firstLviRelativeLocation.Y &&
                    thumbYPos <= lastLviRelativeLocation.Y + lastItemHeight)
                {
                    if (_viewModel.FirstItemDateString == groupName ||
                        _viewModel.LastItemDateString == groupName)
                    {
                        _viewModel.CurrentVisibleItemsDateString = null;
                    }
                    else
                    {
                        _viewModel.CurrentVisibleItemsDateString = groupName;
                    }
                }
            }
        }

        private void LoadItemsAndGroupsCachedInfo(ListView listView)
        {
            if (_lastListViewItemCount == listView.Items.Count)
            {
                return;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);

            if (view.Groups == null)
            {
                return;
            }

            var groups = view.Groups.Cast<CollectionViewGroup>();

            _groupBoundingLvis.Clear();

            if (listView.Items.Count == 0)
            {
                return;
            }

            foreach (var group in groups)
            {
                ListViewItem firstLvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(group.Items.First());
                ListViewItem lastLvi = null;

                if (group.Items.Count > 1)
                {
                    lastLvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(group.Items.Last());
                }

                if (group.Name is string == false)
                {
                    throw new InvalidCastException("Group name should be string.");
                }

                string groupName = (string)group.Name;
                if (_groupBoundingLvis.ContainsKey(groupName))
                {
                    throw new ArgumentException("Group names should be unique");
                }

                _groupBoundingLvis.Add((string)group.Name, new Tuple<ListViewItem, ListViewItem>(firstLvi, lastLvi));
            }

            _viewModel.FirstItemDateString = _groupBoundingLvis.First().Key;
            _viewModel.LastItemDateString = _groupBoundingLvis.Last().Key;

            _lastListViewItemCount = listView.Items.Count;
        }
    }
}
