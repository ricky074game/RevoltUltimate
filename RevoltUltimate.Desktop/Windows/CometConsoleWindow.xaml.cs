using System.Windows;

namespace RevoltUltimate.Desktop.Windows
{
    public partial class CometConsoleWindow : Window
    {
        public CometConsoleWindow()
        {
            InitializeComponent();
        }

        public void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(text + "\n");
                LogScrollViewer.ScrollToEnd();
            });
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}