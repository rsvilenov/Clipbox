using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.CustomControls;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Resources;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.Interfaces;
using Clipbox.Services.Interfaces.Common;
using Clipbox.Services.Interfaces.Common.EventArgsClasses;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for RebuildIndicesWindow.xaml
    /// </summary>
    public partial class RebuildIndicesWindow : WindowFrame
    {
        public ICommand Cancel { get; }
        public ICommand Retry { get; }

        private readonly ISearchService _searchService;

        public event EventHandler<RetryRebuildIndexEventArgs> RetryRequested;

        private string currentClipboardItemId;

        public RebuildIndicesWindow(ISearchService searchService)
        {
            ScaleMode = DpiScaleMode.Full;

            InitializeComponent();

            Cancel = new DelegateCommand(CancelHandler);
            Retry = new DelegateCommand(RetryHandler);

            _searchService = searchService;
            if (_searchService is IProvideProgressInfo progressInfoProvider)
            {
                progressInfoProvider.ProgressChanged += ProgressInfoProvider_ProgressChanged;
            }

            _searchService.StartIndexRebuilding();
            ProgressState = TaskbarItemProgressState.Normal;

            this.mainGrid.DataContext = this;
        }

        public void RetryRebuild()
        {
            ProgressState = TaskbarItemProgressState.Normal;
            ProgressPercentage = 0;
            TaskbarProgressValue = 0;
            HasRebuildingFailed = false;
            _searchService.StartIndexRebuilding();
        }

        private void RetryHandler()
        {
            RetryRequested?.Invoke(this, new RetryRebuildIndexEventArgs(currentClipboardItemId));
        }

        private void ProgressInfoProvider_ProgressChanged(object sender, ProgressEventArgs e)
        {
            currentClipboardItemId = e.CurrentId;

            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    if (e.IsSuccess)
                    {
                        ProgressPercentage = (int)Math.Round((e.Current * 100d) / e.Total);
                        TaskbarProgressValue = ProgressPercentage / 100d;

                        if (ProgressPercentage == 100)
                        {
                            this.Close();
                        }
                    }
                    else
                    {
                        HasRebuildingFailed = true;
                        ProgressState = TaskbarItemProgressState.Error;
                    }
                }));
        }

        private void CancelHandler()
        {
            _searchService.CancelRebuildIndices();
            this.Close();
        }

        public TaskbarItemProgressState ProgressState
        {
            get { return (TaskbarItemProgressState)GetValue(ProgressStateProperty); }
            set { SetValue(ProgressStateProperty, value); }
        }

        public static readonly DependencyProperty ProgressStateProperty =
            DependencyProperty.Register(nameof(ProgressState), typeof(TaskbarItemProgressState), typeof(RebuildIndicesWindow), new PropertyMetadata(TaskbarItemProgressState.None));
        
        public int ProgressPercentage
        {
            get { return (int)GetValue(ProgressPercentageProperty); }
            set { SetValue(ProgressPercentageProperty, value); }
        }

        public static readonly DependencyProperty ProgressPercentageProperty =
            DependencyProperty.Register(nameof(ProgressPercentage), typeof(int), typeof(RebuildIndicesWindow), new PropertyMetadata(0));

        
        /// <summary>
        /// Since TaskbarItemInfo.Value is within the range of 0 to 1,
        /// we need this...
        /// </summary>
        public double TaskbarProgressValue
        {
            get { return (double)GetValue(TaskbarProgressValueProperty); }
            set { SetValue(TaskbarProgressValueProperty, value); }
        }

        public static readonly DependencyProperty TaskbarProgressValueProperty =
            DependencyProperty.Register(nameof(TaskbarProgressValue), typeof(double), typeof(RebuildIndicesWindow), new PropertyMetadata(0d));



        public bool HasRebuildingFailed
        {
            get { return (bool)GetValue(HasRebuildingFailedProperty); }
            set { SetValue(HasRebuildingFailedProperty, value); }
        }
        
        public static readonly DependencyProperty HasRebuildingFailedProperty =
            DependencyProperty.Register(nameof(HasRebuildingFailed), typeof(bool), typeof(RebuildIndicesWindow), new PropertyMetadata(false));
      
    }
}
