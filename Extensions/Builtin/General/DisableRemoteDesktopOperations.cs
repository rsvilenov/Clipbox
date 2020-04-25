using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System.IO;

namespace Clipbox.Extensions.BuiltIn.General
{
    [ClipboxExtensionAttribute]
    public class DisableRemoteDesktopOperations : FlowExtension
    {
        private const string REMOTE_DESKTOP_EXE_NAME = "mstsc.exe";

        public override void OnShowQuickViewWindowRequested(IWindowInfo targetWindowInfo, ref bool cancelShow)
        {
            if (targetWindowInfo.ApplicationInfo.ApplicationPath != null && Path.GetFileName(targetWindowInfo.ApplicationInfo.ApplicationPath).Equals(REMOTE_DESKTOP_EXE_NAME))
            {
                cancelShow = true;
            }
        }

        public override void OnCopyRequested(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList)
        {
            if (sourceWindowInfo.ApplicationInfo.ApplicationPath != null && Path.GetFileName(sourceWindowInfo.ApplicationInfo.ApplicationPath).Equals(REMOTE_DESKTOP_EXE_NAME))
            {
                cancelStoreToItemList = true;
            }
        }
    }
}
