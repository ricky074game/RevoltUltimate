using RevoltUltimate.API.Objects;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Setup.Pages
{
    public partial class UserPage : Page
    {
        private SetupWindow _setupWindowInstance;
        public UserPage(SetupWindow setupWindow)
        {
            InitializeComponent();
            _setupWindowInstance = setupWindow;
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            string userName = UsernameTextBox.Text;

            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User newUser = new User(userName, 0, new List<Game>());

            // Pass the newUser object and the SetupWindow instance to the AccountsPage
            NavigationService?.Navigate(new AccountsPage(_setupWindowInstance, newUser));
        }
    }
}