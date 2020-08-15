using Clipbox.Infrastructure.UI;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls.BindableObjects;
using Clipbox.Views.Settings.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts.HelperControls
{
    /// <summary>
    /// Interaction logic for DepencentExtensionConfirmation.xaml
    /// </summary>
    public partial class DepencentExtensionConfirmation : UserControl
    {
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }

        public event EventHandler<ConfirmationEventArgs> DialogAnswered;

        public DepencentExtensionConfirmation(IEnumerable<ExtensionDisplayInfo> dependentExtensions, bool isDisablingConfirmation)
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            DependentExtensions = new AsyncObservableCollection<ExtensionDisplayInfo>(dependentExtensions);
            IsDisablingConfirmation = isDisablingConfirmation;

            OkClicked = new DelegateCommand(OkButtonHandler);
            CancelClicked = new DelegateCommand(CancelButtonHandler);
        }

        public bool IsDisablingConfirmation
        {
            get { return (bool)GetValue(IsDisablingConfirmationProperty); }
            set { SetValue(IsDisablingConfirmationProperty, value); }
        }

        public static readonly DependencyProperty IsDisablingConfirmationProperty =
            DependencyProperty.Register(nameof(IsDisablingConfirmation), typeof(bool), typeof(DepencentExtensionConfirmation), new PropertyMetadata(false));

        public AsyncObservableCollection<ExtensionDisplayInfo> DependentExtensions
        {
            get { return (AsyncObservableCollection<ExtensionDisplayInfo>)GetValue(DependentExtensionsProperty); }
            set { SetValue(DependentExtensionsProperty, value); }
        }

        public static readonly DependencyProperty DependentExtensionsProperty =
            DependencyProperty.Register(nameof(DependentExtensions), typeof(AsyncObservableCollection<ExtensionDisplayInfo>), typeof(DepencentExtensionConfirmation), new PropertyMetadata(null));



        private void CancelButtonHandler()
        {
            DialogAnswered?.Invoke(this, new ConfirmationEventArgs(false));
        }

        private void OkButtonHandler()
        {
            DialogAnswered?.Invoke(this, new ConfirmationEventArgs(true));
        }
    }
}
