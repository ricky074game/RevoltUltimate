using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RevoltUltimate.API.Accounts
{
    public static class AccountManager
    {
        private static readonly string AccountsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "accounts.json");
        private static readonly byte[] Entropy = { 1, 2, 3, 4, 5, 6, 7, 8 }; // Keep this constant

        static AccountManager()
        {
            Directory.CreateDirectory(path: Path.GetDirectoryName(AccountsFilePath));
        }

        public static void SaveSteamAccount(string username, List<SerializableCookie> cookies)
        {
            var accounts = LoadAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) ?? new SavedAccount { Username = username };

            string cookiesJson = JsonConvert.SerializeObject(cookies);
            account.EncryptedSteamCookies = Protect(cookiesJson);

            if (!accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                accounts.Add(account);
            }

            SaveAccounts(accounts);
        }

        public static void SaveGOGTokens(string username, string accessToken, string refreshToken, string gogUserId)
        {
            var accounts = LoadAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) ?? new SavedAccount { Username = username };

            account.EncryptedGOGAccessToken = Protect(accessToken);
            account.EncryptedGOGRefreshToken = Protect(refreshToken);
            account.GogUserId = gogUserId;

            if (!accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                accounts.Add(account);
            }

            SaveAccounts(accounts);
        }

        public static void DeleteAccount(string username)
        {
            var accounts = LoadAccounts();
            var accountToRemove = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (accountToRemove != null)
            {
                accounts.Remove(accountToRemove);
                SaveAccounts(accounts);
            }
        }

        public static List<SerializableCookie> GetSteamSession(string username)
        {
            var account = GetSavedAccount(username);
            if (account != null && !string.IsNullOrEmpty(account.EncryptedSteamCookies))
            {
                string cookiesJson = Unprotect(account.EncryptedSteamCookies);
                return JsonConvert.DeserializeObject<List<SerializableCookie>>(cookiesJson) ?? new List<SerializableCookie>();
            }
            return new List<SerializableCookie>();
        }

        public static (string AccessToken, string RefreshToken, string GogUserId) GetGOGTokens(string username)
        {
            var account = GetSavedAccount(username);
            if (account != null && !string.IsNullOrEmpty(account.EncryptedGOGAccessToken))
            {
                return (Unprotect(account.EncryptedGOGAccessToken), Unprotect(account.EncryptedGOGRefreshToken), account.GogUserId);
            }
            return (string.Empty, string.Empty, string.Empty);
        }

        public static List<SavedAccount> GetSavedAccounts() => LoadAccounts();

        private static SavedAccount GetSavedAccount(string username) => LoadAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        private static List<SavedAccount> LoadAccounts()
        {
            if (!File.Exists(AccountsFilePath)) return new List<SavedAccount>();

            try
            {
                var json = File.ReadAllText(AccountsFilePath);
                return JsonConvert.DeserializeObject<List<SavedAccount>>(json) ?? new List<SavedAccount>();
            }
            catch
            {
                return new List<SavedAccount>();
            }
        }

        private static void SaveAccounts(List<SavedAccount> accounts)
        {
            var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(AccountsFilePath, json);
        }

        private static string Protect(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var protectedData = ProtectedData.Protect(dataBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedData);
        }

        private static string Unprotect(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            var protectedData = Convert.FromBase64String(data);
            var dataBytes = ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dataBytes);
        }
    }
}