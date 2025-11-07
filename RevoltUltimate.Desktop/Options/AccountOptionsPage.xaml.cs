using RevoltUltimate.API.Accounts;
using RevoltUltimate.Desktop.Setup.Login;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Options
{
    public partial class AccountOptionsPage : UserControl
    {
        public AccountOptionsPage()
        {
            InitializeComponent();
            Loaded += AccountOptionsControl_OnLoaded;
        }

        private void AccountOptionsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAccountStatus();
        }

        private void LoadAccountStatus()
        {
            var savedAccount = AccountManager.GetSavedAccounts().FirstOrDefault();
            if (savedAccount != null)
            {
                SteamAccountStatusText.Text = $"Logged in as: {savedAccount.Username}";
                AddAccountButton.Visibility = Visibility.Collapsed;
                RemoveAccountButton.Visibility = Visibility.Visible;
            }
            else
            {
                SteamAccountStatusText.Text = "No account is linked.";
                AddAccountButton.Visibility = Visibility.Visible;
                RemoveAccountButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveApiSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("API settings saved!");
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new SteamWebLoginWindow();
            loginWindow.ShowDialog();
            LoadAccountStatus();
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            var savedAccount = AccountManager.GetSavedAccounts().FirstOrDefault();
            if (savedAccount != null)
            {
                if (MessageBox.Show($"Are you sure you want to remove the account '{savedAccount.Username}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    AccountManager.DeleteAccount(savedAccount.Username);
                    LoadAccountStatus();
                }
            }
        }
    }
}