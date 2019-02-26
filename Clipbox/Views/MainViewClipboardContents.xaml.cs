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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void subItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
