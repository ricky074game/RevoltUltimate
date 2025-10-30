using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Net.Http;

namespace RevoltUltimate.API.Searcher
{
    public class SteamGameInfo
    {
        [JsonProperty("appid")] public int AppId { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("img_icon_url")] public string ImgIconUrl { get; set; }
    }

    public class SteamApiResponse
    {
        [JsonProperty("response")] public SteamResponseData Response { get; set; }
    }

    public class SteamResponseData
    {
        [JsonProperty("game_count")] public int GameCount { get; set; }

        [JsonProperty("games")] public List<SteamGameInfo> Games { get; set; }
    }

    public class SteamWeb
    {
        private static SteamWeb _instance;

        public static SteamWeb Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(SteamWeb)} has not been initialized. Call {nameof(InitializeSharedInstance)} first.");
                }

                return _instance;
            }
        }

        private string _apiKey;
        private string _steamIdString;
        private SteamWebInterfaceFactory _webInterfaceFactory;
        private PlayerService _playerService;
        private SteamUserStats _steamUserStats;
        private ulong _steamId64;

        public static bool _isReady = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        private SteamWeb(string apiKey, string steamIdString)
        {
            this._apiKey = apiKey;
            this._steamIdString = steamIdString;
            InitializeInternal();
        }

        public async Task<List<Achievement>> GetAchievementsForGame(string gameName)
        {
            if (!IsSteamApiReady)
            {
                Console.WriteLine("Steam Web API not ready.");
                return new List<Achievement>();
            }

            var url =
                $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_apiKey}&steamid={_steamId64}&format=json&include_appinfo=true&include_played_free_games=true&skip_unvetted_apps=false";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);

                var steamGame = apiResponse?.Response?.Games?.FirstOrDefault(g =>
                    g.Name.Equals(gameName, StringComparison.OrdinalIgnoreCase));

                if (steamGame != null)
                {
                    return await GetAchievementsForGameAsync(steamGame.AppId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error getting achievements for game '{gameName}': {e.Message}");
            }

            return new List<Achievement>();
        }

        public static void InitializeSharedInstance(string apiKey, string steamId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine($"{nameof(InitializeSharedInstance)}: API key cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(steamId))
            {
                Console.WriteLine($"{nameof(InitializeSharedInstance)}: Steam ID cannot be null or empty.");
            }

            _instance = new SteamWeb(apiKey, steamId);
        }

        public void Reinitialize(string newApiKey, string newSteamId)
        {
            this._apiKey = newApiKey;
            this._steamIdString = newSteamId;
            InitializeInternal();
        }

        private void InitializeInternal()
        {
            _isReady = false;

            if (!string.IsNullOrEmpty(_apiKey) &&
                !string.IsNullOrEmpty(_steamIdString))
            {
                try
                {
                    _webInterfaceFactory = new SteamWebInterfaceFactory(_apiKey);
                    _playerService = _webInterfaceFactory.CreateSteamWebInterface<PlayerService>();
                    _steamUserStats = _webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>();

                    if (!ulong.TryParse(_steamIdString, out _steamId64))
                    {
                        Console.WriteLine($"Invalid Steam ID format: {_steamIdString}");
                        return;
                    }

                    _isReady = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error initializing SteamWebAPI2 with provided settings: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Steam Web API key or Steam ID is not configured or provided.");
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
                Console.WriteLine("Steam Web API not ready. Ensure it's initialized with valid API key and Steam ID.");
                return new List<Game>();
            }

            var games = new List<Game>();
            var url =
                $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_apiKey}&steamid={_steamId64}&format=json&include_appinfo=true&include_played_free_games=true&&skip_unvetted_apps=false";

            try
            {
                var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                var apiResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);

                if (apiResponse?.Response?.Games != null)
                {
                    // Fetch achievements for all games in parallel
                    var achievementTasks = apiResponse.Response.Games.Select(async steamGameInfo =>
                    {
                        string imageUrl = "";
                        if (!string.IsNullOrEmpty(steamGameInfo.ImgIconUrl) && steamGameInfo.AppId > 0)
                        {
                            imageUrl =
                                $"http://media.steampowered.com/steamcommunity/public/images/apps/{steamGameInfo.AppId}/{steamGameInfo.ImgIconUrl}.jpg";
                        }

                        var game = new Game(steamGameInfo.Name, "Steam", imageUrl, "", "Steam Web API",
                            steamGameInfo.AppId);
                        var achievements = await GetAchievementsForGameAsync(steamGameInfo.AppId).ConfigureAwait(false);
                        if (achievements != null)
                        {
                            game.AddAchievements(achievements);
                        }

                        return game;
                    }).ToList();

                    games = (await Task.WhenAll(achievementTasks).ConfigureAwait(false)).ToList();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error updating games using Steam Web API: {e.Message}");
            }

            return games;
        }

        private async Task<List<Achievement>?> GetAchievementsForGameAsync(int appId)
        {
            if (!_isReady) return null;

            List<Achievement> achievements = new List<Achievement>();
            uint uAppId = (uint)appId;

            try
            {
                var gameSchemaResponse = await _steamUserStats.GetSchemaForGameAsync(uAppId);
                if (gameSchemaResponse?.Data?.AvailableGameStats?.Achievements == null ||
                    !gameSchemaResponse.Data.AvailableGameStats.Achievements.Any())
                {
                    return achievements;
                }

                var playerAchievementsResponse = await _steamUserStats.GetPlayerAchievementsAsync(uAppId, _steamId64);
                var globalPercentagesResponse =
                    await _steamUserStats.GetGlobalAchievementPercentagesForAppAsync(uAppId);
                var globalPercentages = globalPercentagesResponse?.Data?.ToDictionary(p => p.Name, p => p.Percent);

                if (playerAchievementsResponse?.Data?.Achievements != null)
                {
                    var playerAchievements = playerAchievementsResponse.Data.Achievements.ToDictionary(a => a.APIName);
                    var schemaAchievements = gameSchemaResponse.Data.AvailableGameStats.Achievements;

                    for (int i = 0; i < schemaAchievements.Count; i++)
                    {
                        var schemaAch = schemaAchievements.ElementAt(i);
                        bool isUnlocked = false;
                        string unlockTimestamp = "";
                        bool hasProgress = false;
                        int currentProgress = 0;
                        int maxProgress = 0;

                        if (playerAchievements.TryGetValue(schemaAch.Name, out PlayerAchievementModel playerAch))
                        {
                            isUnlocked = playerAch.Achieved == 1;
                            if (isUnlocked && playerAch.UnlockTime != default(DateTime))
                            {
                                if (playerAch.UnlockTime > DateTime.MinValue)
                                {
                                    unlockTimestamp = playerAch.UnlockTime.ToLocalTime().ToString();
                                }
                            }
                        }

                        float globalPercentage = 0;
                        if (globalPercentages != null &&
                            globalPercentages.TryGetValue(schemaAch.Name, out double percentage))
                        {
                            globalPercentage = (float)percentage;
                        }

                        var achievement = new Achievement(
                            schemaAch.DisplayName,
                            schemaAch.Description,
                            schemaAch.Icon,
                            schemaAch.Hidden == 1,
                            i,
                            isUnlocked,
                            unlockTimestamp,
                            1,
                            schemaAch.Name,
                            hasProgress,
                            currentProgress,
                            maxProgress,
                            globalPercentage
                        );
                        achievements.Add(achievement);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Error fetching achievements for game (AppID: {appId}) using SteamWebAPI2: {e.Message}");
            }

            return achievements;
        }

        internal async Task<Game?> GetGameDetailsAsync(int appId)
        {
            if (!_isReady)
            {
                Console.WriteLine("Steam Web API not ready.");
                return null;
            }

            var url =
                $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_apiKey}&steamid={_steamId64}&format=json&include_appinfo=true&include_played_free_games=true&skip_unvetted_apps=false";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);

                var steamGameInfo = apiResponse?.Response?.Games?.FirstOrDefault(g => g.AppId == appId);

                if (steamGameInfo != null)
                {
                    string imageUrl = "";
                    if (!string.IsNullOrEmpty(steamGameInfo.ImgIconUrl) && steamGameInfo.AppId > 0)
                    {
                        imageUrl =
                            $"http://media.steampowered.com/steamcommunity/public/images/apps/{steamGameInfo.AppId}/{steamGameInfo.ImgIconUrl}.jpg";
                    }

                    var game = new Game(steamGameInfo.Name, "Steam", imageUrl, "", "Steam Web API",
                        steamGameInfo.AppId);
                    var achievements = await GetAchievementsForGameAsync(steamGameInfo.AppId);
                    if (achievements != null)
                    {
                        game.AddAchievements(achievements);
                    }

                    return game;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error getting game details for AppID {appId} using Steam Web API: {e.Message}");
            }

            return null;
        }
    }
}