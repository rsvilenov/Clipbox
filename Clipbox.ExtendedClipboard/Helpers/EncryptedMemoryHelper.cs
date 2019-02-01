using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Clipbox.ExtendedClipboard.Helpers
{
    public static class EncryptedMemoryHelper
    {
        // the block size to encrypt with ProtectMemory
        private const int BLOCK_SIZE = 16;

        public static object DectryptDataFromMemory(byte[] data)
        {
            object result = null;

            ProtectedMemory.Unprotect(data, MemoryProtectionScope.SameProcess);

            using (var stream = new MemoryStream(data))
            {
                result = new BinaryFormatter().Deserialize(stream);
            }


            ProtectedMemory.Protect(data, MemoryProtectionScope.SameProcess);

            return result;
        }

        // this is not the most secure type of protection,
        // as the items will be dectypted from time to time,
        // but at least the clipboard items do not stay
        // as plain text in memory all the time
        /// <summary>
        /// Encrypt using the encryption, built-in in Windows
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] EncryptDataInMemory(object data)
        {
            // serialize the data
            byte[] result;
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, data);
                stream.Flush();
                result = stream.ToArray();
            }

            // pad the data - make it suitable for encrption
            // (the following approach to padding has been used by KeePas)
            int numberOfBlocks = result.Length / BLOCK_SIZE;
            if ((numberOfBlocks * BLOCK_SIZE) < result.Length)
            {
                ++numberOfBlocks;
            }

            var dataToProtect = new byte[numberOfBlocks * BLOCK_SIZE];

            Array.Copy(result, dataToProtect, result.Length);

            // encrypt the data
            ProtectedMemory.Protect(dataToProtect, MemoryProtectionScope.SameProcess);

            return dataToProtect;
        }
    }
}
