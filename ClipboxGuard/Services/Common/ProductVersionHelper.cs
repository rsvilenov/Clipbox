using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboxGuard.Services.Common
{
    public static class ProductVersionHelper
    {
        public static Version GetFullProductVersion()
        {
            Version productVersion;
            try
            {
                string versionStr = RegistryUtils.GetProductVersion();
                productVersion = new Version(versionStr);
            }
            catch
            {
                productVersion = ProductConstants.PRODUCT_VERSION;
            }

            return productVersion;
        }
    }
}
