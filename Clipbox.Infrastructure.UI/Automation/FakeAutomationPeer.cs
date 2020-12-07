using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Clipbox.Infrastructure.UI.Automation
{
    /// <summary>
    /// Fake automation peer for resolving the issue with the memory leaks and performance problems,
    /// caused by the automation support for WPF.
    /// </summary>
    /// Some useful links:
    /// https://stackoverflow.com/questions/17297539/can-ui-automation-be-disabled-for-an-entire-wpf-4-0-app
    /// https://www.syncfusion.com/kb/3860/how-to-release-the-memory-held-by-automationpeer-in-wpf-components
    /// https://supportcenter.devexpress.com/ticket/details/t663077/memory-leak-for-comboboxedit-popup-automationpeer
    /// https://supportcenter.devexpress.com/ticket/details/bc4069/devexpress-controls-now-clear-ui-automation-peers-for-the-application
    public class FakeAutomationPeer : FrameworkElementAutomationPeer
    {
        public FakeAutomationPeer(FrameworkElement owner) : base(owner) { }

        protected override string GetNameCore()
        {
            return "CustomWindowAutomationPeer";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Window;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            return new List<AutomationPeer>();
        }
    }
}
