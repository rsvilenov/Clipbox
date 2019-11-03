using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Extentions.BuiltIn.TextFormats.UIExtentions
{
    [ClipboxExtention]
    public class RemoveFormattingUIExtention : UIExtention
    {
        private const string REMOVE_FORMATING_SETTING_KEY = nameof(REMOVE_FORMATING_SETTING_KEY);

        private RemoveFormatting _extentionUI;
        
        internal void ToggleRemoveFormatting(bool isOn)
        {
            SetParameter(FlowParameters.RemoveFormatting, isOn);
        }

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() >= 1 
                && concatinatedItems.All(item => item.ClipboardDataObjects.Any(obj => obj.ClipboardFormatName == "Rich Text Format"));
        }

        public override void OnExtentionViewGenerating(IEnumerable<IClipboardItem> concatinatedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extentionUI = new RemoveFormatting();
            _extentionUI.RemoveFormattingChanged += _extentionUI_RemoveFormattingChanged;
            string toggledStr = GetSetting(REMOVE_FORMATING_SETTING_KEY);
            bool toggled = toggledStr == null ? false : bool.Parse(toggledStr);

            _extentionUI.ToggleRemoveFormatting(toggled);

            ToggleRemoveFormatting(toggled);

            additionalViewElement = _extentionUI;
        }

        private void _extentionUI_RemoveFormattingChanged(object sender, RemoveFormatting.RemoveFormattingEventArgs e)
        {
            ToggleRemoveFormatting(e.Toggled);
            SetSetting(REMOVE_FORMATING_SETTING_KEY, e.Toggled.ToString());
        }
    }
}
