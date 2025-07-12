using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RevoltUltimate.API.Accounts
{
    public static class AccountManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate");
        private static readonly string AccountsFilePath = Path.Combine(AppDataFolder, "accounts.json");
        private static readonly byte[] Entropy = [1, 8, 3, 2, 5];

        static AccountManager()
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        public static List<StoredAccount> GetSavedAccounts()
        {
            if (!File.Exists(AccountsFilePath))
            {
                return new List<StoredAccount>();
            }

            var json = File.ReadAllText(AccountsFilePath);
            return JsonConvert.DeserializeObject<List<StoredAccount>>(json) ?? new List<StoredAccount>();
        }

        public static void SaveAccount(string username, string guardData, string password)
        {
            var accounts = GetSavedAccounts();
            var existingAccount = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (existingAccount != null)
            {
                accounts.Remove(existingAccount);
            }

            var guardDataBytes = System.Text.Encoding.UTF8.GetBytes(guardData);
            var encryptedGuardData = ProtectedData.Protect(guardDataBytes, Entropy, DataProtectionScope.CurrentUser);

            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            var encryptedPassword = ProtectedData.Protect(passwordBytes, Entropy, DataProtectionScope.CurrentUser);

            accounts.Add(new StoredAccount
            {
                Username = username,
                EncryptedGuardData = encryptedGuardData,
                EncryptedPassword = encryptedPassword
            });

            var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(AccountsFilePath, json);
        }

        public static string GetDecryptedGuardData(string username)
        {
            var account = GetSavedAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account?.EncryptedGuardData == null)
            {
                return null;
            }

            try
            {
                var plainTextBytes = ProtectedData.Unprotect(account.EncryptedGuardData, Entropy, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(plainTextBytes);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
        public static string GetDecryptedPassword(string username)
        {
            var account = GetSavedAccounts().FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account?.EncryptedPassword == null)
            {
                return null;
            }

            try
            {
                var plainTextBytes = ProtectedData.Unprotect(account.EncryptedPassword, Entropy, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(plainTextBytes);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public static void DeleteAccount(string username)
        {
            var accounts = GetSavedAccounts();
            var accountToRemove = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (accountToRemove != null)
            {
                accounts.Remove(accountToRemove);
                var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                File.WriteAllText(AccountsFilePath, json);
            }
        }
    }
}
