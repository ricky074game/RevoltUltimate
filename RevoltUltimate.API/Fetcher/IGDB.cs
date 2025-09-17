using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace RevoltUltimate.API.Fetcher
{
    public class IGDB
    {
        private readonly HttpClient _httpClient;
        private const string IgdbApiUrl = "https://api.igdb.com/v4/";
        private const string TwitchAuthUrl = "https://id.twitch.tv/oauth2/token";

        private static readonly (string ClientId, string ClientSecret) TwitchCredentials = ApiKeyManager.LoadTwitchCredentials("RevoltUltimate.API.Fetcher.IGDBApiKey.txt");
        private readonly Task _initializationTask;

        private class TwitchAuthResponse
        {
            [JsonProperty("access_token")]
            public string? AccessToken { get; set; }
        }

        public IGDB()
        {
            _httpClient = new HttpClient();
            _initializationTask = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var authClient = new HttpClient();
            var authUrl = $"{TwitchAuthUrl}?client_id={TwitchCredentials.ClientId}&client_secret={TwitchCredentials.ClientSecret}&grant_type=client_credentials";
            var response = await authClient.PostAsync(authUrl, null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<TwitchAuthResponse>(content);

            if (string.IsNullOrEmpty(authResponse?.AccessToken))
            {
                throw new InvalidOperationException("Failed to obtain access token from Twitch.");
            }

            _httpClient.BaseAddress = new Uri(IgdbApiUrl);
            _httpClient.DefaultRequestHeaders.Add("Client-ID", TwitchCredentials.ClientId);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResponse.AccessToken}");
        }

        public async Task<IEnumerable<SearchResult>?> SearchGamesAsync(string searchQuery)
        {
            await _initializationTask;

            var requestContent = new StringContent($"search \"{searchQuery}\"; fields name, summary, cover.url;", Encoding.UTF8, "text/plain");

            while (true)
            {
                var response = await _httpClient.PostAsync("games", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<SearchResult>>();
                }
                // Consider adding more robust error handling or a limit to retries.
                await Task.Delay(1000);
            }
        }
    }
}