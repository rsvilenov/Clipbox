using Clipbox.Views.Dialog.Enums;
using Clipbox.Views.Dialog.EventArgsClasses;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Clipbox.Views.Dialog
{
    /// <summary>
    /// Interaction logic for DialoguePopup.xaml
    /// </summary>
    public partial class DialogPopup : Popup
    {

        public event EventHandler<DialogAnswerEventArgs> DialogAnswered;

        public DialogButtons DefaultButton
        {
            get { return (DialogButtons)GetValue(DefaultButtonProperty); }
            set { SetValue(DefaultButtonProperty, value); }
        }

        public static readonly DependencyProperty DefaultButtonProperty =
            DependencyProperty.Register(nameof(DefaultButton), typeof(DialogButtons), typeof(DialogPopup), new PropertyMetadata(DialogButtons.Ok));

        
        public DialogTypes DialogueType
        {
            get { return (DialogTypes)GetValue(DialogueTypeProperty); }
            set { SetValue(DialogueTypeProperty, value); }
        }

        public static readonly DependencyProperty DialogueTypeProperty =
            DependencyProperty.Register(nameof(DialogueType), typeof(DialogTypes), typeof(DialogPopup), new PropertyMetadata(DialogTypes.Ok));



        public string DialogText
        {
            get { return (string)GetValue(DialogTextProperty); }
            set { SetValue(DialogTextProperty, value); }
        }
        
        public static readonly DependencyProperty DialogTextProperty =
            DependencyProperty.Register(nameof(DialogText), typeof(string), typeof(DialogPopup), new PropertyMetadata(string.Empty));


        public UIElement Content
        {
            get { return (UIElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(DialogPopup), new PropertyMetadata(null));



        public IInputElement ReturnFocusTo
        {
            get { return (IInputElement)GetValue(ReturnFocusToProperty); }
            set { SetValue(ReturnFocusToProperty, value); }
        }
        
        public static readonly DependencyProperty ReturnFocusToProperty =
            DependencyProperty.Register(nameof(ReturnFocusTo), typeof(IInputElement), typeof(DialogPopup), new PropertyMetadata(null));



        private DialogButtons _focusedButton;

        public DialogPopup()
        {
            InitializeComponent();

            this.mainBorder.DataContext = this;
        }
        
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogAnswered?.Invoke(this, new DialogAnswerEventArgs(DialogButtons.Ok));
            this.IsOpen = false;
        }


        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogAnswered?.Invoke(this, new DialogAnswerEventArgs(DialogButtons.Yes));
            this.IsOpen = false;
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogAnswered?.Invoke(this, new DialogAnswerEventArgs(DialogButtons.No));
            this.IsOpen = false;
        }

        private void BtnYes_GotFocus(object sender, RoutedEventArgs e)
        {
            _focusedButton = DialogButtons.Yes;
        }


        private void BtnNo_GotFocus(object sender, RoutedEventArgs e)
        {
            _focusedButton = DialogButtons.No;
        }


        private void BtnOk_GotFocus(object sender, RoutedEventArgs e)
        {
            _focusedButton = DialogButtons.Ok;
        }

        protected override void OnOpened(EventArgs e)
        {
            if (ReturnFocusTo == null)
            {
                ReturnFocusTo = Keyboard.FocusedElement;
            }

            this.Child.Focusable = true;

            switch (DefaultButton)
            {
                case DialogButtons.No:
                    btnNo.Focus();
                    break;
                case DialogButtons.Yes:
                    btnYes.Focus();
                    break;
                case DialogButtons.Ok:
                    btnOk.Focus();
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ReturnFocusTo != null)
            {
                Keyboard.Focus(ReturnFocusTo);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch( e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.IsOpen = false;
                    break;
                case Key.Enter:
                    e.Handled = true;
                    this.IsOpen = false;
                    DialogAnswered?.Invoke(this, new DialogAnswerEventArgs(_focusedButton));
                    break;
                case Key.Tab:
                case Key.Left:
                case Key.Right:
                    e.Handled = true;
                    if (_focusedButton == DialogButtons.Yes)
                    {
                        btnNo.Focus();
                    }
                    else if (_focusedButton == DialogButtons.No)
                    {
                        btnYes.Focus();
                    }
                    break;
            }
        }
    }
}
