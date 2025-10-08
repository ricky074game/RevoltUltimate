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
        private readonly ConcurrentDictionary<string, Game> _trackedFileGameMap = new ConcurrentDictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

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
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true
                    };

                    watcher.Created += OnFileChanged;
                    watcher.Changed += OnFileChanged;
                    watcher.Renamed += OnFileChanged;

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
        public void StartWatchingSingleFile(Game game, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Trace.WriteLine($"[GameWatcherService] Invalid file path for single file watch: {filePath}");
                return;
            }

            _trackedFileGameMap[filePath] = game;

            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory)) return;

                if (!_watchers.Any(w => w.Path.Equals(directory, StringComparison.OrdinalIgnoreCase)))
                {
                    var watcher = new FileSystemWatcher(directory)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                        Filter = "*.*",
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false
                    };

                    watcher.Changed += OnFileChanged;
                    watcher.Created += OnFileChanged;
                    watcher.Renamed += OnFileRenamed;

                    _watchers.Add(watcher);
                    Trace.WriteLine($"[GameWatcherService] Started watching directory for single files: {directory}");
                }

                Task.Run(() => ProcessAchievementFile(filePath));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GameWatcherService] Error setting up watcher for single file {filePath}: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (_trackedFileGameMap.TryRemove(e.OldFullPath, out var game))
            {
                _trackedFileGameMap[e.FullPath] = game;
                game.trackedFilePath = e.FullPath;
                Trace.WriteLine($"Tracked file '{e.OldName}' was renamed to '{e.Name}'. Path updated.");
            }
            OnFileChanged(sender, e);
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string fullPath = e.FullPath;
            string? name = e.Name;

            if (e is RenamedEventArgs renamedArgs)
            {
                fullPath = renamedArgs.FullPath;
                name = renamedArgs.Name;
            }

            if (string.IsNullOrEmpty(name)) return;

            if (_trackedFileGameMap.ContainsKey(fullPath))
            {
                var game = await ProcessAchievementFile(fullPath);
                if (game != null)
                {
                    GameDataFound?.Invoke(game);
                }
                return;
            }

            if (name.EndsWith("achievements.json", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("Achievements.txt", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("achievement.bin", StringComparison.OrdinalIgnoreCase))
            {
                var game = await ProcessAchievementFile(fullPath);
                if (game != null)
                {
                    GameDataFound?.Invoke(game);
                }
            }
        }

        private async Task<Game?> ProcessAchievementFile(string filePath)
        {
            try
            {
                Game? currentGame;

                if (_trackedFileGameMap.TryGetValue(filePath, out var trackedGame))
                {
                    currentGame = trackedGame;
                    Trace.WriteLine($"Processing manually tracked file '{filePath}' for game '{currentGame.name}'.");
                }
                else
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory == null) return null;
                    var parentDirName = new DirectoryInfo(directory).Name;

                    if (!int.TryParse(parentDirName, out int appId))
                    {
                        Trace.WriteLine($"Could not parse AppID from directory: {parentDirName}");
                        return null;
                    }

                    currentGame = await GetGameData(appId);
                }

                if (currentGame == null) return null;

                Dictionary<string, (bool Unlocked, long UnlockTime)> unlockedStatuses;
                string fileExtension = Path.GetExtension(filePath);

                if (fileExtension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    unlockedStatuses = await ParseJsonAchievements(filePath);
                }
                else if (fileExtension.Equals(".ini", StringComparison.OrdinalIgnoreCase) || fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    unlockedStatuses = await ParseIniAchievements(filePath);
                }
                else
                {
                    return null;
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

        private async Task<Game?> GetGameData(int appId)
        {
            Game? game = null;
            if (SteamWeb._isReady && SteamWeb.Instance.IsSteamApiReady)
            {
                try
                {
                    game = await SteamWeb.Instance.GetGameDetailsAsync(appId);
                    if (game != null)
                    {
                        Trace.WriteLine($"Fetched game data for AppID {appId} from Steam API.");
                        game.method = "Steam Emulator";
                        return game;
                    }
                }
                catch (Exception ex) { Trace.WriteLine($"Steam API fetch failed for AppID {appId}: {ex.Message}."); }
            }

            var localAchievementPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RevoltAchievement", "Achievements", "Steam", $"{appId}.json");
            if (File.Exists(localAchievementPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(localAchievementPath);
                    game = JsonConvert.DeserializeObject<Game>(json);
                    if (game != null)
                    {
                        Trace.WriteLine($"Fetched game data for AppID {appId} from local cache.");
                        game.method = "Steam Emulator";
                        return game;
                    }
                }
                catch (Exception ex) { Trace.WriteLine($"Local cache read failed for AppID {appId}: {ex.Message}."); }
            }

            Trace.WriteLine($"Falling back to web scraping for AppID {appId}.");
            game = await _scraper.ScrapeGameDataAsync(appId);
            if (game != null)
            {
                game.method = "Steam Emulator";
                return game;
            }

            return new Game($"Unknown Game {appId}", "PC", string.Empty, $"A game with AppID {appId}", "Emulator", appId);
        }

        private void UpdateGameAchievements(Game game, Dictionary<string, (bool Unlocked, long UnlockTime)> statuses)
        {
            foreach (var achievement in game.achievements)
            {
                if (statuses.TryGetValue(achievement.apiName, out var status))
                {
                    var unlockedDateTime = status.UnlockTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(status.UnlockTime).ToString("o") : null;
                    achievement.SetUnlockedStatus(status.Unlocked, unlockedDateTime);
                }
            }
        }

        private async Task<Dictionary<string, (bool Unlocked, long UnlockTime)>> ParseJsonAchievements(string filePath)
        {
            var content = await ReadFileWithRetryAsync(filePath);
            if (string.IsNullOrEmpty(content)) return new Dictionary<string, (bool Unlocked, long UnlockTime)>();

            var parsed = JsonConvert.DeserializeObject<Dictionary<string, GseAchievement>>(content);
            return parsed?.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.Earned, kvp.Value.EarnedTime)
            ) ?? new Dictionary<string, (bool Unlocked, long UnlockTime)>();
        }

        private async Task<Dictionary<string, (bool Unlocked, long UnlockTime)>> ParseIniAchievements(string filePath)
        {
            var statuses = new Dictionary<string, (bool Unlocked, long UnlockTime)>();
            var content = await ReadFileWithRetryAsync(filePath);
            if (string.IsNullOrEmpty(content)) return statuses;

            var regex = new Regex(@"\[(.*?)\]\s*HaveAchieved=(\d+)\s*HaveAchievedTime=(\d+)", RegexOptions.Singleline);
            var matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 4)
                {
                    var apiName = match.Groups[1].Value;
                    var unlocked = match.Groups[2].Value == "1";
                    long.TryParse(match.Groups[3].Value, out var unlockTime);
                    statuses[apiName] = (unlocked, unlockTime);
                }
            }
            return statuses;
        }

        private async Task<string> ReadFileWithRetryAsync(string filePath, int maxRetries = 5, int delayMilliseconds = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await File.ReadAllTextAsync(filePath);
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1) throw;
                    await Task.Delay(delayMilliseconds);
                }
            }
            return string.Empty;
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