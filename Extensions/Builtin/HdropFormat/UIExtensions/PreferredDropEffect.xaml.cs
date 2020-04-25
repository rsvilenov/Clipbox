using Clipbox.Extensions.BuiltIn.HdropFormat.Enums;
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

namespace Clipbox.Extensions.BuiltIn.HdropFormat.UIExtensions
{
    /// <summary>
    /// Interaction logic for PreferredDropEffect.xaml
    /// </summary>
    public partial class PreferredDropEffect : UserControl
    {
        internal event EventHandler<SelectedDropEffectEventArgs> DropEffectSelectionChanged;

        public PreferredDropEffect()
        {
            InitializeComponent();
        }

        internal void SetSelectedDropEffect(DropEffects effect)
        {
            switch (effect)
            {
                case DropEffects.DROPEFFECT_COPY:
                    TgCopy.IsChecked = true;
                    TgCut.IsChecked = false;
                    break;
                case DropEffects.DROPEFFECT_MOVE:
                    TgCopy.IsChecked = false;
                    TgCut.IsChecked = true;
                    break;
                default:
                    throw new InvalidOperationException($"Drop effect '{effect}' is not supported yet.");
            }
        }

        private void TgCut_Checked(object sender, RoutedEventArgs e)
        {
            SetSelectedDropEffect(DropEffects.DROPEFFECT_MOVE);
            DropEffectSelectionChanged?.Invoke(this, new SelectedDropEffectEventArgs(DropEffects.DROPEFFECT_MOVE));
        }

        private void TgCopy_Checked(object sender, RoutedEventArgs e)
        {
            SetSelectedDropEffect(DropEffects.DROPEFFECT_COPY);
            DropEffectSelectionChanged?.Invoke(this, new SelectedDropEffectEventArgs(DropEffects.DROPEFFECT_COPY));
        }

        internal class SelectedDropEffectEventArgs : EventArgs
        {
            internal SelectedDropEffectEventArgs(DropEffects effect)
            {
                DropEffect = effect;
            }

            internal DropEffects DropEffect { get; private set; }
        }
    }
}
