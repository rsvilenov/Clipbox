using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.IO;

namespace Clipbox.Extentions.BuiltIn.General
{
    [ClipboxExtention]
    public class DisableRemoteDesktopOperations : FlowExtention
    {
        private const string REMOTE_DESKTOP_EXE_NAME = "mstsc.exe";

        public override void OnShowQuickViewWindowRequested(IWindowInfo targetWindowInfo, ref bool cancelShow)
        {
            if (targetWindowInfo.ApplicationPath != null && Path.GetFileName(targetWindowInfo.ApplicationPath).Equals(REMOTE_DESKTOP_EXE_NAME))
            {
                cancelShow = true;
            }
        }

        public override void OnCopyRequested(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList)
        {
            if (sourceWindowInfo.ApplicationPath != null && Path.GetFileName(sourceWindowInfo.ApplicationPath).Equals(REMOTE_DESKTOP_EXE_NAME))
            {
                cancelStoreToItemList = true;
            }
        }
    }
}
