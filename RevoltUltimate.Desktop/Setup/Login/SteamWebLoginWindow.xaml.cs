using Microsoft.Web.WebView2.Core;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Objects;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;

namespace RevoltUltimate.Desktop.Setup.Login
{
    public partial class SteamWebLoginWindow : Window
    {
        public SteamWebLoginWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await WebView.EnsureCoreWebView2Async(null);
            WebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            WebView.CoreWebView2.Navigate("https://steamcommunity.com/login");
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && (WebView.CoreWebView2.Source.StartsWith("https://steamcommunity.com/id/", StringComparison.OrdinalIgnoreCase) || WebView.CoreWebView2.Source.StartsWith("https://steamcommunity.com/profiles/", StringComparison.OrdinalIgnoreCase) || WebView.CoreWebView2.Source.Equals("https://steamcommunity.com/my/home", StringComparison.OrdinalIgnoreCase)))
            {
                WebView.CoreWebView2.NavigationCompleted -= WebView_NavigationCompleted;
                var steamCommunityCookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://steamcommunity.com");
                var steamStoreCookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://store.steampowered.com");
                var allCookies = steamCommunityCookies.Concat(steamStoreCookies).ToList();
                if (allCookies.Any(c => c.Name == "steamLoginSecure" && !string.IsNullOrEmpty(c.Value)))
                {
                    var serializableCookies = allCookies.Select(c => new SerializableCookie
                    {
                        Name = c.Name,
                        Value = c.Value,
                        Domain = c.Domain,
                        Path = c.Path
                    }).ToList();
                    await StoreAccountSessionAsync(serializableCookies);
                    DialogResult = true;
                    Close();
                }
            }
        }

        private async Task StoreAccountSessionAsync(List<SerializableCookie> cookies)
        {
            try
            {
                var cookieContainer = new System.Net.CookieContainer();
                var handler = new HttpClientHandler { CookieContainer = cookieContainer };
                var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                foreach (var cookie in cookies)
                {
                    try
                    {
                        cookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                    }
                    catch (System.Net.CookieException ex)
                    {
                        Trace.WriteLine($"Skipping invalid cookie '{cookie.Name}': {ex.Message}");
                    }
                }

                var response = await httpClient.GetStringAsync("https://steamcommunity.com/my/profile");
                var personaNameMatch = Regex.Match(response, @"""personaname""\s*:\s*""(.+?)""");

                if (personaNameMatch.Success)
                {
                    string username = personaNameMatch.Groups[1].Value;
                    AccountManager.SaveSteamAccount(username, cookies);
                    Trace.WriteLine($"Session for user '{username}' saved successfully.");
                }
                else
                {
                    Trace.WriteLine("Could not find Persona Name on profile page to save session.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to store account session: {ex.Message}");
            }
        }
    }
}