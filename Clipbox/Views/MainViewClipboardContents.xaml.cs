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
        }

        private void MainViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            //{
                _viewModel.LoadItems();
            //}));
        }
        
        private void PART_VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            double listViewVerticalOffset = scrollBar.Value;
            ListView listView = Traversing.FindParent<ListView>(scrollBar);
            ScrollViewer scrollViewer = Traversing.FindParent<ScrollViewer>(scrollBar);
            foreach (var item in listView.Items)
            {
                ListViewItem lvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(item);
                Point relativeLocation = lvi.TranslatePoint(new Point(0, 0), scrollViewer);
                if (Math.Abs(relativeLocation.Y) + lvi.ActualHeight >= listViewVerticalOffset &&
                    Math.Abs(relativeLocation.Y) <= listViewVerticalOffset  + lvi.ActualHeight)
                {
                    lvi.Foreground = Brushes.Red;
                    var dateCreated = (item as ClipboardViewItem).DateCreated;
                    _viewModel.CurrentVisibleItemsDateString = dateCreated.ToShortDateString();
                }
                else
                {
                    lvi.Foreground = listView.Foreground;
                }
            }
        }


    }
}
