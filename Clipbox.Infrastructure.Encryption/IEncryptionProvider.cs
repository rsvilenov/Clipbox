namespace Clipbox.Infrastructure.Encryption
{
    public interface IEncryptionProvider
    {
        /// <summary>
        /// Encrypt using an auto-generated ket
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Decrypt using the auto-generated key
        /// </summary>
        /// <param name="cryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        byte[] Decrypt(byte[] cryptedData);


        /// <summary>
        /// Encrypt with a password
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="password">Password.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Encrypt(byte[] data, string password);

        /// <summary>
        /// Decrypt with a password
        /// </summary>
        /// <param name="data">The crypted data to decrypt.</param>
        /// <param name="password">Password.</param>
        /// <returns>The decrypted data.</returns>
        byte[] Decrypt(byte[] cryptedData, string password);
    }
}
