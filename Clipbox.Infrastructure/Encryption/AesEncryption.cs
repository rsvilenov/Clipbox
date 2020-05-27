using System;
using System.Security.Cryptography;
using System.Text;

namespace Clipbox.Infrastructure.Encryption
{
    public class AesEncryption : IDisposable
    {
        private RijndaelManaged _aes;

        public AesEncryption(string password)
        {
            byte[] saltBytes = new byte[] { 1, 5, 2, 6, 3, 8, 7, 8 };

            _aes = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128
            };

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
            _aes.Key = key.GetBytes(_aes.KeySize / 8);
            _aes.IV = key.GetBytes(_aes.BlockSize / 8);

            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        private readonly ICryptoTransform _encryptor;
        /// <summary>
        /// Get the ICryptoTransform encryptor to use with CryptoStream
        /// </summary>
        public ICryptoTransform Encryptor
        {
            get
            {
                if (_aes == null)
                {
                    throw new ObjectDisposedException(nameof(AesEncryption));
                }

                return _encryptor;
            }
        }

        private readonly ICryptoTransform _decryptor;
        /// <summary>
        /// Get the ICryptoTransform decryptor to use with CryptoStream
        /// </summary>
        public ICryptoTransform Decryptor
        {
            get
            {
                if (_aes == null)
                {
                    throw new ObjectDisposedException(nameof(AesEncryption));
                }

                return _decryptor;
            }
        }

        public void Dispose()
        {
            if (_aes != null)
            {
                _aes.Dispose();
                _aes = null;
            }
        }
    }
}
