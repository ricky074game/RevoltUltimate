using RevoltUltimate.API.Objects;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace RevoltUltimate.API.Searcher
{
    public class MainSteam
    {
        private static MainSteam _instance;
        public static MainSteam Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(MainSteam)} has not been initialized. Call {nameof(InitializeSharedInstance)} first.");
                }
                return _instance;
            }
        }

        private string _apiKey; // Store API key
        private string _steamIdString; // Store Steam ID string

        private SteamWebInterfaceFactory _webInterfaceFactory;
        private PlayerService _playerService;
        private SteamUserStats _steamUserStats;
        private ulong _steamId64;

        private bool _isReady = false;

        private MainSteam(string apiKey, string steamIdString)
        {
            this._apiKey = apiKey;
            this._steamIdString = steamIdString;
            InitializeInternal();
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
            _instance = new MainSteam(apiKey, steamId);
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

            try
            {
                var ownedGamesResponse = await _playerService.GetOwnedGamesAsync(_steamId64, includeAppInfo: true, includeFreeGames: false);

                if (ownedGamesResponse?.Data?.OwnedGames != null)
                {
                    foreach (var steamGameInfo in ownedGamesResponse.Data.OwnedGames)
                    {
                        string imageUrl = "";
                        if (!string.IsNullOrEmpty(steamGameInfo.ImgIconUrl) && steamGameInfo.AppId > 0)
                        {
                            imageUrl = $"http://media.steampowered.com/steamcommunity/public/images/apps/{steamGameInfo.AppId}/{steamGameInfo.ImgIconUrl}.jpg";
                        }
                        var game = new Game(steamGameInfo.Name, "Steam", imageUrl, "", "Steam Web API");
                        var achievements = await GetAchievementsForGameAsync(game, (int)steamGameInfo.AppId);
                        game.AddAchievements(achievements);
                        games.Add(game);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error updating games using SteamWebAPI2: {e.Message}");
            }
            return games;
        }

        private async Task<List<Achievement>?> GetAchievementsForGameAsync(Game game, int appId)
        {
            if (!_isReady) return null;

            List<Achievement> achievements = new List<Achievement>();

            uint uAppId = (uint)appId;

            try
            {
                var playerAchievementsResponse = await _steamUserStats.GetPlayerAchievementsAsync(uAppId, _steamId64);
                var gameSchemaResponse = await _steamUserStats.GetSchemaForGameAsync(uAppId);

                if (playerAchievementsResponse?.Data?.Achievements != null && gameSchemaResponse?.Data?.AvailableGameStats?.Achievements != null)
                {
                    var playerAchievements = playerAchievementsResponse.Data.Achievements.ToDictionary(a => a.APIName);
                    var schemaAchievements = gameSchemaResponse.Data.AvailableGameStats.Achievements;

                    for (int i = 0; i < schemaAchievements.Count; i++)
                    {
                        var schemaAch = schemaAchievements.ElementAt(i);
                        bool isUnlocked = false;
                        string unlockTimestamp = "";

                        if (playerAchievements.TryGetValue(schemaAch.Name, out PlayerAchievementModel playerAch))
                        {
                            isUnlocked = playerAch.Achieved == 1; // Correctly check if unlocked
                            if (isUnlocked && playerAch.UnlockTime != default(DateTime))
                            {
                                if (playerAch.UnlockTime > DateTime.MinValue)
                                {
                                    unlockTimestamp = playerAch.UnlockTime.ToLocalTime().ToString();
                                }
                            }
                        }

                        var achievement = new Achievement(
                            schemaAch.DisplayName,
                            schemaAch.Description,
                            schemaAch.Icon,
                            schemaAch.Hidden == 1,
                            i,
                            isUnlocked,
                            unlockTimestamp,
                            ""
                        );
                        achievements.Add(achievement);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error fetching achievements for game {game.name} (AppID: {appId}) using SteamWebAPI2: {e.Message}");
            }
            return achievements;
        }
    }
}