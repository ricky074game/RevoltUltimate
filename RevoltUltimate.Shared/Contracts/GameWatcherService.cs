using Newtonsoft.Json;
using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RevoltUltimate.API.Contracts
{
    public class GameWatcherService
    {
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly SteamStoreScraper _scraper = new SteamStoreScraper();

        public event Action<Game> GameDataFound;

        public void StartWatching(List<string> folderPaths)
        {
            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    ScanForExistingFiles(path);

                    var watcher = new FileSystemWatcher(path)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true
                    };

                    watcher.Created += OnFileChanged;
                    watcher.Changed += OnFileChanged;

                    _watchers.Add(watcher);
                    Trace.WriteLine($"Watching for game saves in: {path}");
                }
            }
        }

        private async void ScanForExistingFiles(string path)
        {
            var achievementFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith("achievements.json", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith("Achievements.txt", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith("achievement.bin", StringComparison.OrdinalIgnoreCase));

            foreach (var file in achievementFiles)
            {
                var game = await ProcessAchievementFile(file);
                if (game != null)
                {
                    GameDataFound?.Invoke(game);
                }
            }
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.Name.EndsWith("achievements.json", StringComparison.OrdinalIgnoreCase) ||
                e.Name.EndsWith("Achievements.txt", StringComparison.OrdinalIgnoreCase) ||
                e.Name.EndsWith("achievement.bin", StringComparison.OrdinalIgnoreCase))
            {
                var game = await ProcessAchievementFile(e.FullPath);
                if (game != null)
                {
                    GameDataFound?.Invoke(game);
                }
            }
        }

        private async Task<Game> ProcessAchievementFile(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var parentDirName = new DirectoryInfo(directory).Name;

                if (!int.TryParse(parentDirName, out int appId))
                {
                    Trace.WriteLine($"Could not parse AppID from directory: {parentDirName}");
                    return null;
                }

                Game? currentGame = null;

                // Level 1: Try Steam API
                if (SteamWeb._isReady && SteamWeb.Instance.IsSteamApiReady)
                {
                    try
                    {
                        currentGame = await SteamWeb.Instance.GetGameDetailsAsync(appId);
                        if (currentGame != null)
                        {
                            Trace.WriteLine($"Fetched game data for AppID {appId} from Steam API.");
                            currentGame.method = "Steam Emulator";
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Steam API fetch failed for AppID {appId}: {ex.Message}. Falling back.");
                        currentGame = null;
                    }
                }

                // Level 2: Try Local GitHub Cache
                if (currentGame == null)
                {
                    var localAchievementPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RevoltAchievement", "Achievements", $"{appId}.json");
                    if (File.Exists(localAchievementPath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(localAchievementPath);
                            currentGame = JsonConvert.DeserializeObject<Game>(json);
                            if (currentGame != null)
                            {
                                Trace.WriteLine($"Fetched game data for AppID {appId} from local cache.");
                                currentGame.method = "Steam Emulator";
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Local cache read failed for AppID {appId}: {ex.Message}. Falling back.");
                            currentGame = null;
                        }
                    }
                }

                // Level 3: Fallback to Steam Scraping
                if (currentGame == null)
                {
                    Trace.WriteLine($"Falling back to web scraping for AppID {appId}.");
                    currentGame = await _scraper.ScrapeGameDataAsync(appId);
                    if (currentGame != null)
                    {
                        currentGame.method = "Steam Emulator";
                    }
                }

                // If all methods fail, create a placeholder
                if (currentGame == null)
                {
                    currentGame = new Game($"Unknown Game {appId}", "PC", null, $"A game with AppID {appId}", "Emulator", new List<Achievement>(), appId);
                }

                Dictionary<string, (bool Unlocked, long UnlockTime)> unlockedStatuses;
                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    unlockedStatuses = ParseJsonAchievements(filePath);
                }
                else
                {
                    unlockedStatuses = ParseIniAchievements(filePath);
                }

                UpdateGameAchievements(currentGame, unlockedStatuses);
                return currentGame;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error processing achievement file {filePath}: {ex.Message}");
                return null;
            }
        }

        private void UpdateGameAchievements(Game game, Dictionary<string, (bool Unlocked, long UnlockTime)> statuses)
        {
            foreach (var achievement in game.achievements)
            {
                if (statuses.TryGetValue(achievement.apiName, out var status))
                {
                    var unlockedDateTime = DateTimeOffset.FromUnixTimeSeconds(status.UnlockTime).ToString("o");
                    achievement.SetUnlockedStatus(status.Unlocked, unlockedDateTime);
                }
            }
        }

        private Dictionary<string, (bool Unlocked, long UnlockTime)> ParseJsonAchievements(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var parsed = JsonConvert.DeserializeObject<Dictionary<string, GseAchievement>>(content);
            return parsed.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.Earned, kvp.Value.EarnedTime)
            );
        }

        private Dictionary<string, (bool Unlocked, long UnlockTime)> ParseIniAchievements(string filePath)
        {
            var statuses = new Dictionary<string, (bool Unlocked, long UnlockTime)>();
            var content = File.ReadAllText(filePath);
            var regex = new Regex(@"\[(.*?)\]\s*HaveAchieved=(\d+)\s*HaveAchievedTime=(\d+)", RegexOptions.Singleline);
            var matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                var apiName = match.Groups[1].Value;
                var unlocked = match.Groups[2].Value == "1";
                long.TryParse(match.Groups[3].Value, out var unlockTime);
                statuses[apiName] = (unlocked, unlockTime);
            }
            return statuses;
        }

        public void StopWatching()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
        }
    }
}