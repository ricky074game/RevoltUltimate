using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Controls
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
        private void BusyIndicatorGif_MediaEnded(object sender, RoutedEventArgs e)
        {
            BusyIndicatorGif.Position = System.TimeSpan.Zero;
            BusyIndicatorGif.Play();
        }
    }
}