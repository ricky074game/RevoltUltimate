using System.Windows;

namespace RevoltUltimate.Desktop
{
    public partial class BootScreen : Window
    {
        public BootScreen()
        {
            InitializeComponent();
        }

        public void UpdateProgress(double value)
        {
            ProgressBar.Value = value;
        }

        public void UpdateStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}