using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.General.FlowExtensions
{
    [ClipboxExtension(
        description: "Cancel copying from blacklisted apps",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
        )]
    public class CancelCopyOfBlacklistedAppsFlowExtension : FlowExtension
    {
        public override void OnCopyRequested(IWindowInfo sourceWindowInfo, ref bool cancelStoreToItemList)
        {
            string appBlackList = GetSetting<string>(ExtensionSettingNames.BlacklistedApps);
            if (string.IsNullOrEmpty(appBlackList))
            {
                return;
            }

            string sourceAppPath = sourceWindowInfo.ApplicationInfo?.ApplicationPath;
            if (string.IsNullOrEmpty(sourceAppPath))
            {
                return;
            }

            cancelStoreToItemList = appBlackList
                .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(path => sourceAppPath.StartsWith(path));
        }
    }
}
