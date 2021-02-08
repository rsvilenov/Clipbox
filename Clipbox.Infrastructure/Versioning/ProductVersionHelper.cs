using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using System;

namespace Clipbox.Infrastructure.Versioning
{
    public static class ProductVersionHelper
    {
        private static Version _productVersion;
        public static Version GetFullProductVersion()
        {
            if (_productVersion == null)
            {
                try
                {
                    string versionStr = RegistryUtils.GetProductVersion();
                    _productVersion = new Version(versionStr);
                }
                catch
                {
                    _productVersion = ProductConstants.PRODUCT_VERSION;
                }
            }

            return _productVersion;
        }

        public static void EnsureVersionConsistency()
        {
            try
            {
                string versionStr = RegistryUtils.GetProductVersion();
                if (!string.IsNullOrEmpty(versionStr) && versionStr.Split('.').Length < 3)
                {
                    throw new FormatException($"The version string in the registry is malformatted: {versionStr}");
                }

                _productVersion = new Version(versionStr);
            }
            catch
            {
#if DEBUG
                // the product is probably not installed
                return;
#else
                throw;
#endif
            }

            if (_productVersion.Major != ProductConstants.PRODUCT_VERSION.Major
                && _productVersion.Minor != ProductConstants.PRODUCT_VERSION.Minor)
            {
                throw new InvalidOperationException("The product version constant does not match the version info in the registry. Verify that the Major and Minor numbers in the version constant in the installer are the same with those in the version constant in the Core project.");
            }
        }
    }
}
