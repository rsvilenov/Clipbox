using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Clipbox.Win32
{
    public static class TokenUtils
    {
        private enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern int WTSQueryUserToken(int sessionId, ref IntPtr token);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(
        IntPtr hToken,
        TokenInformationClass tokenInfoClass,
        IntPtr pTokenInfo,
        Int32 tokenInfoLength,
        out Int32 returnLength);

        /// <summary>
        /// Gets the primary token for the user. This token can be used with
        /// CreateProcess* functions. No duplication of the token is required.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static IntPtr GetUserTokenForSession(int sessionId)
        {
            var hToken = IntPtr.Zero;
            if (WTSQueryUserToken(sessionId, ref hToken) == 0)
            {
                throw new Win32Exception();
            }
            return hToken;
        }

        /// <summary>
        /// Based on https://code.msdn.microsoft.com/CSUACSelfElevation-644673d3/sourcecode?fileId=21729&pathId=1041468146
        /// </summary>
        /// <param name="hToken"></param>
        /// <returns></returns>
        public static IntPtr TryGetElevatedToken(IntPtr hToken)
        {
            // Allocate a buffer for the elevation type information. 
            int cbSize = sizeof(TokenElevationType);
            IntPtr pElevationType = Marshal.AllocHGlobal(cbSize);
            if (pElevationType == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            // Retrieve token elevation type information. 
            if (!GetTokenInformation(hToken,
                TokenInformationClass.TokenElevationType, pElevationType,
                cbSize, out cbSize))
            {
                throw new Win32Exception();
            }

            // Marshal the TOKEN_ELEVATION_TYPE enum from native to .NET. 
            TokenElevationType elevType = (TokenElevationType)
                Marshal.ReadInt32(pElevationType);

            IntPtr hElevatedToken = IntPtr.Zero;
            // If limited, get the linked elevated token for further check. 
            if (elevType == TokenElevationType.TokenElevationTypeLimited)
            {
                //Console.WriteLine("TokenElevationTypeLimited");
                // Allocate a buffer for the linked token. 
                cbSize = IntPtr.Size;
                IntPtr pLinkedToken = Marshal.AllocHGlobal(cbSize);
                if (pLinkedToken == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                // Get the linked token. 
                if (!GetTokenInformation(hToken,
                    TokenInformationClass.TokenLinkedToken, pLinkedToken,
                    cbSize, out cbSize))
                {
                    throw new Win32Exception();
                }

                // Marshal the linked token value from native to .NET. 
                hElevatedToken = Marshal.ReadIntPtr(pLinkedToken);
            }
            else if (elevType == TokenElevationType.TokenElevationTypeFull)
            {
                hElevatedToken = hToken;
            }

            return hElevatedToken;
        }
    }
}
