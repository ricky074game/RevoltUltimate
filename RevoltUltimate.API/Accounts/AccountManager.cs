using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RevoltUltimate.API.Accounts
{
    public static class AccountManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "steam_accounts.json");
        private static readonly byte[] Entropy = { 5, 8, 2, 1, 4 };

        public static void SaveSteamSession(string username, string sessionId, string steamLoginSecure)
        {
            var accounts = GetSavedAccounts();
            var existingAccount = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (existingAccount != null)
            {
                existingAccount.EncryptedSessionId = Encrypt(sessionId);
                existingAccount.EncryptedSteamLoginSecure = Encrypt(steamLoginSecure);
            }
            else
            {
                accounts.Add(new SavedAccount
                {
                    Username = username,
                    EncryptedSessionId = Encrypt(sessionId),
                    EncryptedSteamLoginSecure = Encrypt(steamLoginSecure),
                    EncryptedPassword = null // Explicitly null for session-only login
                });
            }

            SaveAccountsToFile(accounts);
        }

        public static Tuple<string, string> GetSteamSession(string username)
        {
            var account = GetSavedAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account == null || string.IsNullOrEmpty(account.EncryptedSessionId) || string.IsNullOrEmpty(account.EncryptedSteamLoginSecure))
            {
                return null;
            }

            string sessionId = Decrypt(account.EncryptedSessionId);
            string steamLoginSecure = Decrypt(account.EncryptedSteamLoginSecure);

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(steamLoginSecure))
            {
                return null;
            }

            return new Tuple<string, string>(steamLoginSecure, sessionId);
        }

        public static void ClearSteamSession(string username)
        {
            var accounts = GetSavedAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account != null)
            {
                account.EncryptedSessionId = null;
                account.EncryptedSteamLoginSecure = null;
                // If the account only had session data and no password, we can remove it.
                if (string.IsNullOrEmpty(account.EncryptedPassword))
                {
                    accounts.Remove(account);
                }
                SaveAccountsToFile(accounts);
            }
        }

        public static void SaveAccount(string username, string password, string guardData = null)
        {
            var accounts = GetSavedAccounts();
            var existingAccount = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (existingAccount != null)
            {
                existingAccount.EncryptedPassword = Encrypt(password);
                existingAccount.GuardData = guardData;
            }
            else
            {
                accounts.Add(new SavedAccount
                {
                    Username = username,
                    EncryptedPassword = Encrypt(password),
                    GuardData = guardData
                });
            }

            SaveAccountsToFile(accounts);
        }

        public static void UpdateGuardData(string username, string guardData)
        {
            var accounts = GetSavedAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account != null)
            {
                account.GuardData = guardData;
                SaveAccountsToFile(accounts);
            }
        }

        private static void SaveAccountsToFile(List<SavedAccount> accounts)
        {
            string json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            string directoryPath = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(FilePath, json);
        }

        public static List<SavedAccount> GetSavedAccounts()
        {
            if (!File.Exists(FilePath))
            {
                return new List<SavedAccount>();
            }
            string json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<List<SavedAccount>>(json) ?? new List<SavedAccount>();
        }

        public static string? GetDecryptedPassword(string username)
        {
            var account = GetSavedAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return account != null ? Decrypt(account.EncryptedPassword) : null;
        }

        public static string? GetGuardData(string username)
        {
            var account = GetSavedAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return account?.GuardData;
        }

        private static string Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            byte[] plainBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string Decrypt(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData)) return null;
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Return null if decryption fails
                return null;
            }
        }

        public static void DeleteAccount(string username)
        {
            var accounts = GetSavedAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account != null)
            {
                accounts.Remove(account);
                SaveAccountsToFile(accounts);
            }
        }
    }
}