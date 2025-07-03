using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Setup.Steam;
using SteamKit2;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Setup
{
    public partial class SteamLogin : Window
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        private readonly SteamLocal _steamLocal;
        private Window _confirmationWindow;

        public SteamLogin()
        {
            InitializeComponent();
            var wpfAuthenticator = new WpfAuthenticator(this.Dispatcher);
            wpfAuthenticator.OnDeviceConfirmationRequired += ShowConfirmationWindow;
            _steamLocal = new SteamLocal(wpfAuthenticator);
        }
        private void ShowConfirmationWindow(Window window)
        {
            _confirmationWindow = window;
            _confirmationWindow.Owner = this;
            _confirmationWindow.Show();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void SignInButton_OnClick(object sender, RoutedEventArgs e)
        {
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Please enter both username and password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UsernameTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
            PasswordBox.BorderBrush = new SolidColorBrush(Colors.Gray);

            var signingInWindow = new SigningInWindow { Owner = this };
            signingInWindow.Show();

            try
            {
                var result = await _steamLocal.Login(Username, Password);

                if (result == EResult.OK)
                {
                    MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close(); // This will also close owned windows
                }
                else
                {
                    MessageBox.Show($"Login failed: {result}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UsernameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    PasswordBox.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ensure all helper windows are closed
                signingInWindow.Close();
                _confirmationWindow?.Close();
            }
        }
    }
}