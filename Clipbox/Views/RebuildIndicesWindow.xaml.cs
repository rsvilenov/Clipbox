using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Resources;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Services.Interfaces.Common;
using Clipbox.Services.Interfaces.Common.EventArgsClasses;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for RebuildIndicesWindow.xaml
    /// </summary>
    public partial class RebuildIndicesWindow : AcryllicWindow
    {
        public ICommand Cancel { get; }

        private readonly ISearchService _searchService;

        public RebuildIndicesWindow(ISearchService searchService)
        {
            InitializeComponent();

            using (Bitmap bmpIcon = ResourceUtils.GetApplicationIcon())
            {
                Icon = bmpIcon.ToBitmapSource();
            }

            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, 
                (s, e) => SystemCommands.MinimizeWindow(this)));

            Cancel = new DelegateCommand(CancelHandler);
            _searchService = searchService;
            if (_searchService is IProvideProgressInfo progressInfoProvider)
            {
                progressInfoProvider.ProgressChanged += ProgressInfoProvider_ProgressChanged;
            }

            _searchService.StartIndexRebuilding();

            this.mainGrid.DataContext = this;
        }

        private void ProgressInfoProvider_ProgressChanged(object sender, ProgressEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    ProgressPercentage = (int)Math.Round((e.Current * 100d) / e.Total);

                    if (ProgressPercentage == 100)
                    {
                        this.Close();
                    }
                }));
        }

        private void CancelHandler()
        {
            _searchService.CancelRebuildIndices();
            this.Close();
        }
        
        public int ProgressPercentage
        {
            get { return (int)GetValue(ProgressPercentageProperty); }
            set { SetValue(ProgressPercentageProperty, value); }
        }

        public static readonly DependencyProperty ProgressPercentageProperty =
            DependencyProperty.Register(nameof(ProgressPercentage), typeof(int), typeof(RebuildIndicesWindow), new PropertyMetadata(0));
        

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
