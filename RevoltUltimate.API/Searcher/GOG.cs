using HtmlAgilityPack;
using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace RevoltUltimate.API.Searcher
{
    public class GOG
    {
        private static GOG _instance;
        public static GOG Instance => _instance ??= new GOG();

        private const string ClientId = "46899977096215655";
        private const string ClientSecret = "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9";
        private const string AuthTokenUrl = "https://auth.gog.com/token";
        private const string EmbedApiUrl = "https://embed.gog.com";
        private readonly HttpClient _httpClient;

        private string _accessToken;
        private string _refreshToken;
        private string _userId;
        private bool _isLoggedIn;
        private bool _searching = false;

        public class GOGTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
            [JsonProperty("user_id")]
            public string UserId { get; set; }
        }

        public class GOGUserData
        {
            [JsonProperty("username")]
            public string Username { get; set; }
            [JsonProperty("userId")]
            public string UserId { get; set; }
        }

        private class OwnedGamesResponse
        {
            [JsonProperty("owned")]
            public List<long> Owned { get; set; } = new List<long>();
        }

        private class GameDetailsResponse
        {
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("backgroundImage")]
            public string BackgroundImage { get; set; }
        }

        private class GogAchievementsResponse
        {
            [JsonProperty("items")]
            public List<GogAchievementItem> Items { get; set; }
        }

        private class GogAchievementItem
        {
            [JsonProperty("achievement_key")]
            public string AchievementKey { get; set; }
            [JsonProperty("visible")]
            public bool Visible { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("image_url_unlocked")]
            public string? ImageUrlUnlocked { get; set; }
            [JsonProperty("date_unlocked")]
            public DateTime? DateUnlocked { get; set; }
        }

        private class GogDbBuildDetailsResponse
        {
            [JsonProperty("clientId")]
            public string ClientId { get; set; }
        }

        private GOG()
        {
            _httpClient = new HttpClient();
        }

        public void SetSession(string accessToken, string refreshToken, string userId)
        {
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(userId))
            {
                _isLoggedIn = false;
                return;
            }
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _userId = userId;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            _isLoggedIn = true;
        }

        public void ResetSession()
        {
            _accessToken = null;
            _refreshToken = null;
            _userId = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _isLoggedIn = false;
        }

        public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                Trace.WriteLine("No refresh token available to refresh session.");
                ResetSession();
                return false;
            }

            string requestUrl = $"{AuthTokenUrl}?client_id={ClientId}&client_secret={ClientSecret}&grant_type=refresh_token&refresh_token={_refreshToken}";
            try
            {
                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    var tokenResponse = JsonConvert.DeserializeObject<GOGTokenResponse>(jsonResponse);
                    if (tokenResponse != null)
                    {
                        SetSession(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.UserId);
                        Trace.WriteLine("GOG session has been refreshed successfully.");
                        return true;
                    }
                }

                Trace.WriteLine("Failed to refresh GOG session. The refresh token may be invalid.");
                ResetSession();
                return false;
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Token refresh operation was canceled.");
                return false;
            }
        }

        public async Task<GOGTokenResponse?> ExchangeAuthCodeForTokensAsync(string authCode, CancellationToken cancellationToken = default)
        {
            string redirectUri = "https://embed.gog.com/on_login_success?origin=client";
            string requestUrl = $"{AuthTokenUrl}?client_id={ClientId}&client_secret={ClientSecret}&grant_type=authorization_code&code={authCode}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}";
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<GOGTokenResponse>(jsonResponse);
        }

        public async Task<GOGUserData?> GetUserDataAsync(CancellationToken cancellationToken = default)
        {
            if (!_isLoggedIn) return null;
            var response = await _httpClient.GetAsync($"{EmbedApiUrl}/userData.json", cancellationToken);
            if (response is { IsSuccessStatusCode: false, StatusCode: System.Net.HttpStatusCode.Unauthorized })
            {
                bool refreshed = await RefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    response = await _httpClient.GetAsync($"{EmbedApiUrl}/userData.json", cancellationToken);
                }
            }
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<GOGUserData>(jsonResponse);
        }

        public async Task<List<Game>> GetOwnedGamesWithAchievementsAsync(CancellationToken cancellationToken = default)
        {
            if (_searching)
            {
                Trace.WriteLine("GOG search is already in progress.");
                return null;
            }

            _searching = true;
            try
            {
                var games = new List<Game>();
                if (!_isLoggedIn) return games;

                var ownedGamesResponse = await _httpClient.GetAsync($"{EmbedApiUrl}/user/data/games", cancellationToken);
                if (!ownedGamesResponse.IsSuccessStatusCode)
                {
                    if (ownedGamesResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        bool refreshed = await RefreshTokenAsync(cancellationToken);
                        if (refreshed)
                        {
                            ownedGamesResponse = await _httpClient.GetAsync($"{EmbedApiUrl}/user/data/games", cancellationToken);
                        }
                        else
                        {
                            Trace.WriteLine("The auth code has expired and refresh failed.");
                            return games;
                        }
                    }
                    else
                    {
                        return games;
                    }
                }

                var ownedGamesContent = await ownedGamesResponse.Content.ReadAsStringAsync(cancellationToken);
                var ownedGamesData = JsonConvert.DeserializeObject<OwnedGamesResponse>(ownedGamesContent);

                if (ownedGamesData?.Owned == null) return games;

                foreach (var gameId in ownedGamesData.Owned)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    HttpResponseMessage detailsResponse = null;
                    try
                    {
                        detailsResponse = await _httpClient.GetAsync($"{EmbedApiUrl}/account/gameDetails/{gameId}.json", cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.WriteLine($"GetOwnedGamesWithAchievementsAsync for gameId {gameId} was canceled.");
                        throw;
                    }

                    if (detailsResponse.IsSuccessStatusCode)
                    {
                        var detailsContent = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
                        if (string.IsNullOrWhiteSpace(detailsContent) || detailsContent.Trim().StartsWith("["))
                        {
                            Trace.WriteLine($"Skipping gameId {gameId} due to empty or array response for game details.");
                            continue;
                        }
                        var gameDetails = JsonConvert.DeserializeObject<GameDetailsResponse>(detailsContent);

                        if (gameDetails != null && !string.IsNullOrEmpty(gameDetails.Title))
                        {
                            var game = new Game(
                                name: gameDetails.Title,
                                platform: "GOG",
                                imageUrl: gameDetails.BackgroundImage != null
                                    ? $"https:{gameDetails.BackgroundImage}"
                                    : "",
                                description: "",
                                method: "GOG",
                                appid: gameId
                            );

                            var achievements = await GetPlayerAchievementsAsync(gameId, cancellationToken);
                            game.AddAchievements(achievements);
                            games.Add(game);
                        }
                    }
                    else if (detailsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        bool refreshed = await RefreshTokenAsync(cancellationToken);
                        if (refreshed)
                        {
                            detailsResponse = await _httpClient.GetAsync($"{EmbedApiUrl}/account/gameDetails/{gameId}.json", cancellationToken);
                            if (detailsResponse.IsSuccessStatusCode)
                            {
                                var detailsContent = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
                                if (string.IsNullOrWhiteSpace(detailsContent) || detailsContent.Trim().StartsWith("["))
                                {
                                    Trace.WriteLine($"Skipping gameId {gameId} after refresh due to empty or array response for game details.");
                                    continue;
                                }
                                var gameDetails = JsonConvert.DeserializeObject<GameDetailsResponse>(detailsContent);

                                if (gameDetails != null && !string.IsNullOrEmpty(gameDetails.Title))
                                {
                                    var game = new Game(
                                        name: gameDetails.Title,
                                        platform: "GOG",
                                        imageUrl: gameDetails.BackgroundImage != null
                                            ? $"https:{gameDetails.BackgroundImage}"
                                            : "",
                                        description: "",
                                        method: "GOG",
                                        appid: gameId
                                    );

                                    var achievements = await GetPlayerAchievementsAsync(gameId, cancellationToken);
                                    game.AddAchievements(achievements);
                                    games.Add(game);
                                }
                                continue;
                            }
                        }
                        break;
                    }
                }
                return games;
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("GetOwnedGamesWithAchievementsAsync operation was canceled.");
                return null;
            }
            finally
            {
                _searching = false;
            }
        }

        private async Task<string> GetClientIdFromGogDbAsync(long gameId, CancellationToken cancellationToken = default)
        {
            try
            {
                var buildsPageUrl = $"https://www.gogdb.org/data/products/{gameId}/builds";
                var buildsPageResponse = await _httpClient.GetStringAsync(buildsPageUrl, cancellationToken);
                var doc = new HtmlDocument();
                doc.LoadHtml(buildsPageResponse);
                var buildIdNodes = doc.DocumentNode.SelectNodes("//table//a[contains(@href, '.json')]");
                if (buildIdNodes == null || !buildIdNodes.Any())
                {
                    return null;
                }
                var latestBuild = buildIdNodes
                    .Select(node => node.GetAttributeValue("href", ""))
                    .Where(href => !string.IsNullOrEmpty(href) && href.EndsWith(".json"))
                    .Select(href =>
                    {
                        string numberPart = href.Replace(".json", "");
                        bool success = long.TryParse(numberPart, out long buildNumber);
                        return new { Href = href, BuildNumber = buildNumber, Success = success };
                    })
                    .Where(x => x.Success)
                    .OrderByDescending(x => x.BuildNumber)
                    .FirstOrDefault();
                if (latestBuild == null)
                {
                    return null;
                }
                var buildDetailsUrl = $"{buildsPageUrl}/{latestBuild.Href}";
                var buildDetailsResponse = await _httpClient.GetStringAsync(buildDetailsUrl, cancellationToken);
                var buildDetails = JsonConvert.DeserializeObject<GogDbBuildDetailsResponse>(buildDetailsResponse);

                return buildDetails?.ClientId;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Trace.WriteLine($"No data found on gogdb.org for gameId {gameId}.");
                return null;
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine($"GetClientIdFromGogDbAsync for gameId {gameId} was canceled.");
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"An error occurred in GetClientIdFromGogDbAsync for gameId {gameId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Achievement>> GetPlayerAchievementsAsync(long gameId, CancellationToken cancellationToken = default)
        {
            var achievements = new List<Achievement>();
            if (!_isLoggedIn) return achievements;

            var clientId = await GetClientIdFromGogDbAsync(gameId, cancellationToken);
            if (string.IsNullOrEmpty(clientId)) return achievements;

            var url = $"https://gameplay.gog.com/clients/{clientId}/users/{_userId}/achievements";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response is { IsSuccessStatusCode: false, StatusCode: System.Net.HttpStatusCode.Unauthorized })
                {
                    bool refreshed = await RefreshTokenAsync(cancellationToken);
                    if (refreshed)
                    {
                        response = await _httpClient.GetAsync(url, cancellationToken);
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var achievementsResponse = JsonConvert.DeserializeObject<GogAchievementsResponse>(content);
                    if (achievementsResponse?.Items != null)
                    {
                        achievements.AddRange(achievementsResponse.Items.Select((item, id) => new Achievement(
                            Name: item.Name,
                            Description: item.Description,
                            ImageUrl: item.ImageUrlUnlocked,
                            Hidden: !item.Visible,
                            Id: id,
                            Unlocked: item.DateUnlocked.HasValue,
                            DateTimeUnlocked: item.DateUnlocked?.ToString("o"),
                            Difficulty: 1,
                            apiName: item.AchievementKey,
                            progress: false,
                            currentProgress: 0,
                            maxProgress: 0,
                            getglobalpercentage: 0
                        )));
                    }
                }

                return achievements;
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine($"GameId {gameId} was canceled.");
                return achievements;
            }
        }
    }
}