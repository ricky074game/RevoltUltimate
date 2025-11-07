using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace RevoltUltimate.Desktop
{
    public class ApplicationSettings
    {
        public string Version { get; set; } = "0.1";

        public string? CustomAnimationDllPath { get; set; }

        [JsonIgnore]
        private byte[]? EncryptedSteamApiKey { get; set; }

        [JsonIgnore]
        private byte[]? EncryptedSteamId { get; set; }

        private static readonly byte[] Entropy = { 1, 4, 8, 2, 5 };

        public List<String> WatchedFolders { get; set; }


        public string? SteamApiKey
        {
            get => Decrypt(EncryptedSteamApiKey);
            set => EncryptedSteamApiKey = Encrypt(value);
        }
        public string? SteamId
        {
            get => Decrypt(EncryptedSteamId);
            set => EncryptedSteamId = Encrypt(value);
        }
        [JsonProperty(nameof(EncryptedSteamApiKey))]
        public string? EncryptedSteamApiKeyBase64
        {
            get => EncryptedSteamApiKey != null ? Convert.ToBase64String(EncryptedSteamApiKey) : null;
            set => EncryptedSteamApiKey = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }

        [JsonProperty(nameof(EncryptedSteamId))]
        public string? EncryptedSteamIdBase64
        {
            get => EncryptedSteamId != null ? Convert.ToBase64String(EncryptedSteamId) : null;
            set => EncryptedSteamId = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }

        public ApplicationSettings()
        {
            if (string.IsNullOrEmpty(Version))
            {
                Version = "0.1";
            }
        }
        private byte[]? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        }
        private string? Decrypt(byte[]? encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0) return null;
            try
            {
                var plainBytes = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Handle decryption failure (e.g., data corruption or access by a different user)
                return null;
            }
        }
    }
}