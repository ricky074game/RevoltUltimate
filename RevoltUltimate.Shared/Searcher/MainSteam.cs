using RevoltUltimate.API.Objects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RevoltUltimate.API.Searcher
{
    internal class SteamOwnedGamesResponse
    {
        [JsonPropertyName("response")]
        public OwnedGamesData Response { get; set; }
    }

    internal class OwnedGamesData
    {
        [JsonPropertyName("game_count")]
        public int GameCount { get; set; }

        [JsonPropertyName("games")]
        public List<SteamGameInfo> Games { get; set; }
    }

    internal class SteamGameInfo
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("img_icon_url")]
        public string ImgIconUrl { get; set; }
    }

    internal class SteamPlayerAchievementsResponse
    {
        [JsonPropertyName("playerstats")]
        public PlayerStatsData PlayerStats { get; set; }
    }

    internal class PlayerStatsData
    {
        [JsonPropertyName("steamID")]
        public string SteamID { get; set; }

        [JsonPropertyName("gameName")]
        public string GameName { get; set; } // Game name as known by Steam

        [JsonPropertyName("achievements")]
        public List<SteamPlayerAchievement> Achievements { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    internal class SteamPlayerAchievement
    {
        [JsonPropertyName("apiname")]
        public string ApiName { get; set; }

        [JsonPropertyName("achieved")]
        public int Achieved { get; set; } // 0 or 1

        [JsonPropertyName("unlocktime")]
        public long UnlockTime { get; set; } // Unix timestamp
    }

    internal class SteamGameSchemaResponse
    {
        [JsonPropertyName("game")]
        public GameSchemaData Game { get; set; }
    }

    internal class GameSchemaData
    {
        [JsonPropertyName("gameName")]
        public string GameName { get; set; }

        [JsonPropertyName("availableGameStats")]
        public AvailableGameStats Stats { get; set; }
    }

    internal class AvailableGameStats
    {
        [JsonPropertyName("achievements")]
        public List<SteamAchievementSchema> Achievements { get; set; }
    }

    internal class SteamAchievementSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("hidden")]
        public int Hidden { get; set; } // 0 or 1

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("icongray")]
        public string IconGray { get; set; }
    }

    public class MainSteam
    {
        private static readonly MainSteam _instance = new MainSteam();
        public static MainSteam Instance => _instance;

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _steamId;

        private bool _isReady = false;

        private MainSteam()
        {
            _httpClient = new HttpClient();
            _apiKey = "";
            _steamId = "76561198887014425";

            if (!string.IsNullOrEmpty(_apiKey) && _apiKey != "YOUR_API_KEY" &&
                !string.IsNullOrEmpty(_steamId) && _steamId != "USER_STEAM_ID")
            {
                _isReady = true;
            }
            else
            {
                Console.WriteLine("Steam Web API key or Steam ID is not configured.");
            }
        }

        public bool IsSteamApiReady => _isReady;

        public Task<List<Game>> Update()
        {
            return UpdateGamesAsync();
        }

        private async Task<List<Game>> UpdateGamesAsync()
        {
            if (!_isReady)
            {
                Console.WriteLine("Steam Web API not ready. Check API key and Steam ID.");
                return new List<Game>();
            }

            var games = new List<Game>();
            string url = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_apiKey}&steamid={_steamId}&format=json&include_appinfo=1";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                SteamOwnedGamesResponse ownedGamesResponse = JsonSerializer.Deserialize<SteamOwnedGamesResponse>(jsonResponse, options);

                if (ownedGamesResponse?.Response?.Games != null)
                {
                    foreach (var steamGameInfo in ownedGamesResponse.Response.Games)
                    {
                        string imageUrl = "";
                        if (!string.IsNullOrEmpty(steamGameInfo.ImgIconUrl) && steamGameInfo.AppId > 0)
                        {
                            imageUrl = $"http://media.steampowered.com/steamcommunity/public/images/apps/{steamGameInfo.AppId}/{steamGameInfo.ImgIconUrl}.jpg";
                        }
                        var game = new Game(steamGameInfo.Name, "Steam", imageUrl, "", "Steam Web API");
                        await GetAchievementsForGameAsync(game, steamGameInfo.AppId);
                        games.Add(game);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine($"JSON parsing error: {e.Message}");
            }
            catch (Exception e) // Catch any other unexpected errors
            {
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
            }
            return games;
        }

        private async Task GetAchievementsForGameAsync(Game game, int appId)
        {
            if (!_isReady) return;

            // Step 1: Get player's achievement status for the game
            string playerAchievementsUrl = $"http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={appId}&key={_apiKey}&steamid={_steamId}&format=json";
            // Step 2: Get global achievement percentages (contains names, descriptions, icons)
            string gameSchemaUrl = $"http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={_apiKey}&appid={appId}&format=json";

            try
            {
                // Fetch player achievements
                HttpResponseMessage playerResponse = await _httpClient.GetAsync(playerAchievementsUrl);
                playerResponse.EnsureSuccessStatusCode();
                string playerJsonResponse = await playerResponse.Content.ReadAsStringAsync();
                var playerAchievementsData = JsonSerializer.Deserialize<SteamPlayerAchievementsResponse>(playerJsonResponse);

                // Fetch game schema for achievement details
                HttpResponseMessage schemaResponse = await _httpClient.GetAsync(gameSchemaUrl);
                schemaResponse.EnsureSuccessStatusCode();
                string schemaJsonResponse = await schemaResponse.Content.ReadAsStringAsync();
                var gameSchemaData = JsonSerializer.Deserialize<SteamGameSchemaResponse>(schemaJsonResponse);

                if (playerAchievementsData?.PlayerStats?.Achievements != null && gameSchemaData?.Game?.Stats?.Achievements != null)
                {
                    var playerAchievements = playerAchievementsData.PlayerStats.Achievements.ToDictionary(a => a.ApiName);
                    var schemaAchievements = gameSchemaData.Game.Stats.Achievements;

                    for (int i = 0; i < schemaAchievements.Count; i++)
                    {
                        var schemaAch = schemaAchievements[i];
                        bool isUnlocked = false;
                        string unlockTimestamp = "";

                        if (playerAchievements.TryGetValue(schemaAch.Name, out SteamPlayerAchievement playerAch))
                        {
                            isUnlocked = playerAch.Achieved == 1;
                            if (isUnlocked && playerAch.UnlockTime > 0)
                            {
                                unlockTimestamp = DateTimeOffset.FromUnixTimeSeconds(playerAch.UnlockTime).LocalDateTime.ToString();
                            }
                        }

                        var achievement = new Achievement(
                            schemaAch.DisplayName,
                            schemaAch.Description,
                            schemaAch.Icon,
                            schemaAch.Hidden == 1,
                            i, // Using loop index as ID, similar to original code
                            isUnlocked,
                            unlockTimestamp,
                            "" // Difficulty is not a standard Steam achievement property
                        );
                        game.AddAchievement(achievement);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error for game {game.Name} (AppID: {appId}): {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine($"JSON parsing error for game {game.Name} (AppID: {appId}): {e.Message}");
            }
            catch (Exception e) // Catch any other unexpected errors
            {
                Console.WriteLine($"An unexpected error occurred while fetching achievements for game {game.Name} (AppID: {appId}): {e.Message}");
            }
        }
    }
}