using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipbox.Views.FilterBand
{
    public abstract class FilterDialogBase : UserControl
    {
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public FilterDialogBase()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                throw new InvalidOperationException("This constructor should never be called from a user code.");
            }
        }

        public FilterDialogBase(IClipboardItemsDataProvider clipboardItemsDataProvider)
        {
            ClipboardItemsDataProvider = clipboardItemsDataProvider;

            Loaded += FilterDialogBase_Loaded;

            OkClicked = new DelegateCommand(() => OnOkButton());
            CancelClicked = new DelegateCommand(() => OnCancelButton());
        }

        private void FilterDialogBase_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            IsLoading = true;

            Task.Run(() =>
            {
                LoadData();

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        OnLoadDataComplete();
                        IsLoading = false;
                    }));
            });

        }

        /// <summary>
        /// Indicates that the code in LoadData() is executing
        /// </summary>
        protected bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        protected static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(FilterDialogBase), new PropertyMetadata(false));

        
        protected IClipboardItemsDataProvider ClipboardItemsDataProvider { get; private set; }

        /// <summary>
        /// Raised when the OkClicked command is executed
        /// </summary>
        protected abstract void OnOkButton();

        /// <summary>
        /// Raised when the CancelClicked command is executed
        /// </summary>
        protected abstract void OnCancelButton();

        /// <summary>
        /// Put the long-running operations here
        /// </summary>
        protected abstract void LoadData();

        /// <summary>
        /// This executes in the UI thread, so it is safe to assign 
        /// bindable properties safely here (i.e. collections)
        /// </summary>
        protected abstract void OnLoadDataComplete();
    }
}
