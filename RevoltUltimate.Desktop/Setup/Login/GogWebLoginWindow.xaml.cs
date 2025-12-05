using Microsoft.Web.WebView2.Core;
using System.Windows;

namespace RevoltUltimate.Desktop.Setup.Login
{
    public partial class GOGWebLoginWindow : Window
    {
        public string? AuthCode { get; private set; }

        public GOGWebLoginWindow()
        {
            InitializeComponent();
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("https://embed.gog.com/on_login_success", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(e.Uri);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                AuthCode = query["code"];

                if (!string.IsNullOrEmpty(AuthCode))
                {
                    DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to retrieve GOG authentication code.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    this.Close();
                }
            }
        }
    }
}