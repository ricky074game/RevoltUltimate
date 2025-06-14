using RevoltUltimate.API.Objects; // Required for User
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Setup.Pages
{
    public partial class AccountsPage : Page
    {
        private SetupWindow _setupWindowInstance;
        private User _currentUser;

        // Modified constructor to accept SetupWindow instance and User object
        public AccountsPage(SetupWindow setupWindow, User user)
        {
            InitializeComponent();
            _setupWindowInstance = setupWindow;
            _currentUser = user;
            // You can optionally display user information here if needed
            // For example: WelcomeMessage.Text = $"Hello, {_currentUser.UserName}!";
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            _setupWindowInstance?.CompleteSetupAndLaunchMainApplication(_currentUser);
        }
    }
}