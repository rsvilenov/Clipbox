using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Clipbox.Win32
{
    public static class AssemblyUtils
    {
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        private static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);
        
        public static void AssertManagedAssemblySecurity(string assemblyPath)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            bool isVerified = false;
            if (!StrongNameSignatureVerificationEx(executingAssembly.Location, true, ref isVerified))
            {
                throw new Win32Exception();
            }

            // if the caller is not signed, do not check the callee
            if (!isVerified)
            {
                return;
            }

            Assembly newAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            isVerified = false;
            if (!StrongNameSignatureVerificationEx(executingAssembly.Location, true, ref isVerified))
            {
                throw new Win32Exception();
            }

            bool success = executingAssembly.GetName().GetPublicKey() ==
                newAssembly.GetName().GetPublicKey();

            if (!success)
            {
                throw new SecurityException($"The certificate for assembly {assemblyPath} failed to validate");
            }
        }

        public static void AssertAssemblyHashCode(string assemblyPath, byte[] hashCode)
        {
            byte[] assemblyHash = SHA256.Create(assemblyPath).Hash;
            if (assemblyHash != hashCode)
            {
                throw new SecurityException($"The hashcode for assembly {assemblyPath} failed to validate");
            }
        }

    }
}
