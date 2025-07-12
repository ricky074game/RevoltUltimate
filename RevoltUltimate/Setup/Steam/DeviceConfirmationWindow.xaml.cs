using System.Windows;

namespace RevoltUltimate.Desktop.Setup.Steam
{
    public partial class DeviceConfirmationWindow : Window
    {

        public DeviceConfirmationWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
