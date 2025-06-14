using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Setup.Pages
{
    public partial class WelcomePage : Page
    {
        private readonly SetupWindow _setupWindow;

        public WelcomePage(SetupWindow setupWindow)
        {
            _setupWindow = setupWindow;
            InitializeComponent();
        }

        private void GetStarted_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new UserPage(_setupWindow));
        }
    }
}