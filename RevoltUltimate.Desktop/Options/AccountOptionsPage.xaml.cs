using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Setup;
using RevoltUltimate.Desktop.Setup.Login;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Options
{
    public partial class AccountOptionsPage : UserControl
    {
        private ObservableCollection<AccountDisplayItem> _accounts = new();

        public AccountOptionsPage()
        {
            InitializeComponent();
            Loaded += AccountOptionsPage_Loaded;
        }

        private void AccountOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAccountsList();
        }

        private void RefreshAccountsList()
        {
            _accounts.Clear();

            var savedAccounts = AccountManager.GetSavedAccounts();

            foreach (var account in savedAccounts)
            {
                var steamCookies = AccountManager.GetSteamSession(account.Username);
                if (steamCookies.Any())
                {
                    _accounts.Add(new AccountDisplayItem
                    {
                        Username = account.Username,
                        PlatformName = "Steam (Web)",
                        PlatformType = AccountPlatformType.SteamWeb,
                        PlatformIcon = "S",
                        PlatformColor = new SolidColorBrush(Color.FromRgb(27, 40, 56))
                    });
                }
                var gogTokens = AccountManager.GetGOGTokens(account.Username);
                if (!string.IsNullOrEmpty(gogTokens.AccessToken))
                {
                    _accounts.Add(new AccountDisplayItem
                    {
                        Username = account.Username,
                        PlatformName = "GOG Galaxy",
                        PlatformType = AccountPlatformType.GOG,
                        PlatformIcon = "G",
                        PlatformColor = new SolidColorBrush(Color.FromRgb(134, 50, 138))
                    });
                }
            }
            if (App.Settings != null && !string.IsNullOrWhiteSpace(App.Settings.SteamApiKey))
            {
                _accounts.Add(new AccountDisplayItem
                {
                    Username = $"Steam ID: {App.Settings.SteamId}",
                    PlatformName = "Steam (API)",
                    PlatformType = AccountPlatformType.SteamApi,
                    PlatformIcon = "A",
                    PlatformColor = new SolidColorBrush(Color.FromRgb(27, 40, 56))
                });
            }

            AccountsItemsControl.ItemsSource = _accounts;
            EmptyStatePanel.Visibility = _accounts.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            AddAccountPopup.IsOpen = !AddAccountPopup.IsOpen;
        }

        private void AddSteamWebAccount_Click(object sender, RoutedEventArgs e)
        {
            AddAccountPopup.IsOpen = false;

            var loginWindow = new SteamWebLoginWindow
            {
                Owner = Window.GetWindow(this)
            };
            loginWindow.ShowDialog();
            RefreshAccountsList();
        }

        private void AddSteamApiAccount_Click(object sender, RoutedEventArgs e)
        {
            AddAccountPopup.IsOpen = false;

            var fields = new List<InputFieldDefinition>
            {
                new InputFieldDefinition
                {
                    Label = "Steam API Key",
                    DefaultValue = App.Settings?.SteamApiKey,
                    Placeholder = "Enter your Steam API key",
                    Validator = value => !string.IsNullOrWhiteSpace(value),
                    ValidationErrorMessage = "Steam API Key is required."
                },
                new InputFieldDefinition
                {
                    Label = "Steam ID",
                    DefaultValue = App.Settings?.SteamId,
                    Placeholder = "Enter your Steam ID (e.g., 76561198...)",
                    Validator = value => !string.IsNullOrWhiteSpace(value),
                    ValidationErrorMessage = "Steam ID is required."
                }
            };

            var dialog = new InputDialog(
                "Enter your Steam Web API credentials to sync game data and achievements.",
                fields,
                "https://steamcommunity.com/dev/apikey")
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                if (App.Settings != null)
                {
                    App.Settings.SteamApiKey = dialog.Responses["Steam API Key"];
                    App.Settings.SteamId = dialog.Responses["Steam ID"];
                    SaveSettings();
                    RefreshAccountsList();
                    Trace.WriteLine("Steam API settings saved.");
                }
            }
        }

        private async void AddGOGAccount_Click(object sender, RoutedEventArgs e)
        {
            AddAccountPopup.IsOpen = false;

            var gogLoginWindow = new GOGWebLoginWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (gogLoginWindow.ShowDialog() == true && !string.IsNullOrEmpty(gogLoginWindow.AuthCode))
            {
                try
                {
                    var tokenResponse = await GOG.Instance.ExchangeAuthCodeForTokensAsync(gogLoginWindow.AuthCode);
                    GOG.Instance.SetSession(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.UserId);

                    var userData = await GOG.Instance.GetUserDataAsync();
                    if (userData != null && !string.IsNullOrEmpty(userData.Username))
                    {
                        AccountManager.SaveGOGTokens(userData.Username, tokenResponse.AccessToken,
                            tokenResponse.RefreshToken, tokenResponse.UserId);
                        RefreshAccountsList();
                        Trace.WriteLine($"GOG account '{userData.Username}' linked.");
                    }
                    else
                    {
                        throw new Exception("Failed to retrieve GOG user data.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to link GOG account: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.WriteLine($"Failed to link GOG account: {ex.Message}");
                }
            }
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AccountDisplayItem account)
            {
                string message = account.PlatformType == AccountPlatformType.SteamApi
                    ? "Are you sure you want to remove the Steam API configuration?"
                    : $"Are you sure you want to remove '{account.Username}' ({account.PlatformName})?";

                if (MessageBox.Show(message, "Confirm Removal",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    RemoveAccountByType(account);
                    RefreshAccountsList();
                }
            }
        }

        private void RemoveAccountByType(AccountDisplayItem account)
        {
            switch (account.PlatformType)
            {
                case AccountPlatformType.SteamWeb:
                    // Clear Steam session
                    AccountManager.SaveSteamAccount(account.Username, new List<SerializableCookie>());
                    SteamScrape.Instance.Disconnect();

                    // Delete account if no other platforms
                    var gogTokens = AccountManager.GetGOGTokens(account.Username);
                    if (string.IsNullOrEmpty(gogTokens.AccessToken))
                    {
                        AccountManager.DeleteAccount(account.Username);
                    }
                    Trace.WriteLine($"Steam Web account '{account.Username}' removed.");
                    break;

                case AccountPlatformType.SteamApi:
                    if (App.Settings != null)
                    {
                        App.Settings.SteamApiKey = null;
                        App.Settings.SteamId = null;
                        SaveSettings();
                    }
                    Trace.WriteLine("Steam API configuration removed.");
                    break;

                case AccountPlatformType.GOG:
                    AccountManager.SaveGOGTokens(account.Username, string.Empty, string.Empty, string.Empty);
                    GOG.Instance.ResetSession();
                    var steamCookies = AccountManager.GetSteamSession(account.Username);
                    if (!steamCookies.Any())
                    {
                        AccountManager.DeleteAccount(account.Username);
                    }
                    Trace.WriteLine($"GOG account '{account.Username}' removed.");
                    break;
            }
        }

        private void SaveSettings()
        {
            try
            {
                string settingsFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevoltUltimate", "settings.json");

                string? directoryPath = Path.GetDirectoryName(settingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (App.Settings != null)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(App.Settings,
                        Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(settingsFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving settings: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public enum AccountPlatformType
    {
        SteamWeb,
        SteamApi,
        GOG
    }

    public class AccountDisplayItem
    {
        public string Username { get; set; } = string.Empty;
        public string PlatformName { get; set; } = string.Empty;
        public AccountPlatformType PlatformType { get; set; }
        public string PlatformIcon { get; set; } = string.Empty;
        public SolidColorBrush PlatformColor { get; set; } = Brushes.Gray;
    }
}