namespace Clipbox.Infrastructure.Encryption
{
    public static class DpApiEncryption
    {
        public static byte[] Protect(byte[] bytes)
        {
            return System.Security.Cryptography.ProtectedData.Protect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }

        public static byte[] Unprotect(byte[] bytes)
        {
            return System.Security.Cryptography.ProtectedData.Unprotect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }
    }
}
