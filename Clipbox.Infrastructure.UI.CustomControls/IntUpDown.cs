using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_BtnUp", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_BtnDown", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    public class IntUpDown : Control
    {
        static IntUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IntUpDown), new FrameworkPropertyMetadata(typeof(IntUpDown)));
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public readonly static DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(IntUpDown), new PropertyMetadata(int.MaxValue));



        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public readonly static DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(IntUpDown), new PropertyMetadata(int.MinValue));


        public int? Value
        {
            get { return (int?)GetValue(ValueProperty); }
            set { SetCurrentValue(ValueProperty, value); }
        }

        public readonly static DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int?), typeof(IntUpDown), new PropertyMetadata(0, (s, args) =>
            {
                IntUpDown tb = (IntUpDown)s;
                tb.SetUpDownButtonsEnabledDisabledStatus();
                tb.RaiseValueChangedEvent(args);
            }));

        public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;
        private void RaiseValueChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }


        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        public readonly static DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(int), typeof(IntUpDown), new UIPropertyMetadata(1));



        RepeatButton _btnUp;
        RepeatButton _btnDown;
        TextBox _textBox;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _btnUp = Template.FindName("PART_BtnUp", this) as RepeatButton;
            _btnDown = Template.FindName("PART_BtnDown", this) as RepeatButton;
            _textBox = Template.FindName("PART_TextBox", this) as TextBox;

            _textBox.Text = Minimum.ToString();

            _btnUp.Click += _btnUp_Click;
            _btnDown.Click += _btnDown_Click;
            _textBox.TextChanged += _textBox_TextChanged;
            _textBox.PreviewTextInput += _textBox_PreviewTextInput;
            _textBox.MouseWheel += _textBox_MouseWheel;

            SetUpDownButtonsEnabledDisabledStatus();
        }

        private void _textBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (Value + 1 <= Maximum)
                {
                    Value++;
                }
            }
            else
            {
                if (Value - 1 >= Minimum)
                {
                    Value--;
                }
            }
        }

        private void _textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (e.Text == null || e.Text.Length == 0)
            {
                e.Handled = true;
                return;
            }

            if (e.Text.All(c => char.IsNumber(c)) == false)
            {
                e.Handled = true;
                return;
            }

            int newValue = 0;

            if (tb.Text != null && tb.Text.Length > 0)
            {
                if (tb.SelectionLength > 0)
                {
                    string finalValue = tb.Text.Substring(0, tb.SelectionStart) + e.Text;
                    if (tb.SelectionLength < tb.Text.Length)
                    {
                        finalValue += tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);
                    }

                    newValue = int.Parse(finalValue);
                }
                else
                {
                    string finalValue = tb.Text.Trim().Insert(tb.CaretIndex, e.Text.Trim());
                    newValue = int.Parse(finalValue);
                }
            }
            else
            {
                newValue = int.Parse(e.Text);
            }

            if (newValue > Maximum
                    || newValue < Minimum)
            {
                e.Handled = true;
            }
        }

        private void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Value == null)
            {
                Value = Minimum;
            }
        }

        private void SetUpDownButtonsEnabledDisabledStatus()
        {
            if (_btnDown != null)
            {
                _btnDown.IsEnabled = Value > Minimum;
            }

            if (_btnUp != null)
            {
                _btnUp.IsEnabled = Value < Maximum;
            }
        }

        private void _btnUp_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += Step;
                if (Value > Maximum)
                {
                    Value = Maximum;
                }
            }
        }

        private void _btnDown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= Step;
                if (Value < Minimum)
                {
                    Value = Minimum;
                }
            }
        }
    }
}
