using Microsoft.Web.WebView2.Core;
using RevoltUltimate.API.Accounts;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;

namespace RevoltUltimate.Desktop.Setup.Steam
{
    public partial class SteamWebLoginWindow : Window
    {
        public string SessionId { get; private set; }
        public string SteamLoginSecure { get; private set; }

        public SteamWebLoginWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async(null);
            WebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                var cookies = await GetSteamCookiesAsync();
                var sessionIdCookie = cookies.FirstOrDefault(c => c.Name == "sessionid");
                var steamLoginSecureCookie = cookies.FirstOrDefault(c => c.Name == "steamLoginSecure");

                if (steamLoginSecureCookie != null && !string.IsNullOrEmpty(steamLoginSecureCookie.Value))
                {
                    SessionId = sessionIdCookie?.Value;
                    SteamLoginSecure = steamLoginSecureCookie.Value;

                    await StoreAccountSessionAsync(SessionId, SteamLoginSecure);

                    Close();
                }
            }
        }

        private async Task StoreAccountSessionAsync(string sessionId, string steamLoginSecure)
        {
            try
            {
                var cookieContainer = new System.Net.CookieContainer();
                var handler = new HttpClientHandler { CookieContainer = cookieContainer };
                var httpClient = new HttpClient(handler);

                cookieContainer.Add(new System.Net.Cookie("sessionid", sessionId, "/", ".steamcommunity.com"));
                cookieContainer.Add(new System.Net.Cookie("steamLoginSecure", steamLoginSecure, "/", ".steamcommunity.com"));

                var response = await httpClient.GetStringAsync("https://steamcommunity.com/my");
                var personaNameMatch = Regex.Match(response, @"""personaname""\s*:\s*""(.+?)""");

                if (personaNameMatch.Success)
                {
                    string username = personaNameMatch.Groups[1].Value;
                    AccountManager.SaveSteamSession(username, sessionId, steamLoginSecure);
                    System.Diagnostics.Debug.WriteLine($"Session for user '{username}' saved successfully.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Could not find Persona Name on profile page to save session.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to store account session: {ex.Message}");
                // Optionally, show an error to the user
            }
        }


        private async Task<List<CoreWebView2Cookie>> GetSteamCookiesAsync()
        {
            return await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://steamcommunity.com/login");
        }
    }
}