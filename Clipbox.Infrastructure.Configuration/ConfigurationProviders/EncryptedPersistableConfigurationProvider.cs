using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Persistence;

namespace Clipbox.Infrastructure.Configuration
{
    public class EncryptedPersistableConfigurationProvider : PersistableConfigurationProvider
    {
        private readonly IEncryptionProvider _encryptionProvider;

        public EncryptedPersistableConfigurationProvider(
            IPersistenceProvider persistenceProvider, 
            IEncryptionProvider encryptionProvider)
            : base(persistenceProvider)
        {
            
            _encryptionProvider = encryptionProvider;
        }

        protected override byte[] Serialize(SettingObject settingObject)
        {
            return _encryptionProvider.Encrypt(base.Serialize(settingObject));
        }

        protected override SettingObject Deserialize(byte[] serialized)
        {
            return base.Deserialize(_encryptionProvider.Decrypt(serialized));
        }

        protected override string GetKeyStableHash(ISettingKey key)
        {
            return MD5Hash.Create(string.Concat("e_", key.Name, key.TargetComponentType?.FullName ?? string.Empty));
        }
    }
}
