using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class NotificationBell : UserControl
    {
        public NotificationBell()
        {
            InitializeComponent();
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
        }
    }
}