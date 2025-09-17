using Newtonsoft.Json;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.API.Update;
using RevoltUltimate.Desktop.Setup;
using RevoltUltimate.Desktop.Setup.Steam;
using RevoltUltimate.Desktop.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RevoltUltimate.Desktop
{
    public partial class App : Application
    {
        public static User? CurrentUser { get; set; }
        private static string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");
        public static bool IsDebugMode { get; private set; }
        private static string UserFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "user.json");
        private const string SetupCompleteEventName = "RevoltUltimateSetupCompleteEvent";
        public static ApplicationSettings? Settings { get; private set; }
        private const string CurrentSettingsVersion = "0.1";
        private DispatcherTimer _sessionRefreshTimer;
        private static TextBoxTraceListener _globalTraceListener;
        private GameWatcherService? _gameWatcherService;


        private async void Application_Startup(object sender, StartupEventArgs e)
        {
#if DEBUG
            IsDebugMode = true;
#endif
            if (e.Args.Contains("/debug"))
            {
                IsDebugMode = true;
            }
            _globalTraceListener = new TextBoxTraceListener();
            Trace.Listeners.Add(_globalTraceListener);
            Trace.WriteLine("Application has begun startup.");
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var bootScreen = new BootScreen();
            bootScreen.Show();

            try
            {
                bootScreen.UpdateStatus("Loading user data...");
                bootScreen.UpdateProgress(5);
                LoadUser();
                if (CurrentUser == null)
                {
                    await RunSetup(bootScreen);
                }
                bootScreen.UpdateStatus("Loading Settings...");
                bootScreen.UpdateProgress(10);
                UpdateSettings();
                if (Settings == null)
                {
                    MessageBox.Show("Settings file is missing or corrupted. The application will now open setup.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await RunSetup(bootScreen);
                }
                bootScreen.UpdateProgress(50);

                await SetupLinkers(bootScreen);

                bootScreen.UpdateProgress(90);
                bootScreen.UpdateStatus("Checking for updates...");
                await CheckForUpdates(bootScreen);

                MainWindow mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Loaded += MainWindow_Loaded;
                mainWindow.Show();
                bootScreen.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during startup: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("MainWindow loaded. Initializing GameWatcherService.");
            _gameWatcherService = new GameWatcherService();
            _gameWatcherService.GameDataFound += OnGameDataFound;
            if (Settings?.WatchedFolders != null && Settings.WatchedFolders.Any())
            {
                _gameWatcherService.StartWatching(Settings.WatchedFolders);
            }

            // Re-watch manually tracked files from the previous session
            if (CurrentUser?.Games != null)
            {
                foreach (var game in CurrentUser.Games)
                {
                    if (!string.IsNullOrEmpty(game.trackedFilePath))
                    {
                        WatchSingleFile(game, game.trackedFilePath);
                    }
                }
            }
        }

        private async Task CheckForUpdates(BootScreen bootScreen)
        {
            var downloader = new GitDownloader();
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RevoltAchievement");
            if (Path.Exists(localPath))
            {
                await Task.Run(() => downloader.PullLatestChanges(localPath));
            }
            else
            {
                await Task.Run(() =>
                    downloader.CloneRepositoryAsync("https://github.com/ricky074game/RevoltAchievement", localPath,
                        new Progress<string>(), new Progress<int>()));
            }
        }
        private void SetupSessionRefreshTimer()
        {
            _sessionRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(6)
            };
            _sessionRefreshTimer.Tick += async (s, e) =>
            {
                Debug.WriteLine("Timer Ticked: Attempting to refresh Steam session.");
                await SteamScrape.Instance.TryRefreshSessionAsync();
            };
            _sessionRefreshTimer.Start();
        }

        private void UpdateSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);

                    Settings = JsonConvert.DeserializeObject<ApplicationSettings>(json);

                    if (Settings != null && Settings.Version == CurrentSettingsVersion)
                    {
                        Debug.WriteLine("Settings loaded successfully.");
                    }
                    else if (Settings != null && Settings.Version != CurrentSettingsVersion)
                    {
                        MessageBox.Show($"Settings file is an outdated version ({Settings.Version}). Resetting to default settings for version {CurrentSettingsVersion}.",
                            "Settings Update", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetToDefaultSettings();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                    MessageBox.Show($"An unexpected error occurred while loading settings: {ex.Message}. Resetting to default settings.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetToDefaultSettings();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Settings file not found. Resetting to default settings.");
                ResetToDefaultSettings();
            }
        }

        private void ResetToDefaultSettings()
        {
            var defaultFolders = new List<string>
            {
                Environment.ExpandEnvironmentVariables(@"%PUBLIC%\Documents\Steam\CODEX"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\Goldberg SteamEmu Saves"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\EMPRESS"),
                Environment.ExpandEnvironmentVariables(@"%PUBLIC%\EMPRESS"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\Steam\Codex"),
                Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\Steam"),
                Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\SKIDROW"),
                Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\SkidRow"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\SmartSteamEmu"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\CreamAPI"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\GSE Saves")
            };

            Settings = new ApplicationSettings
            {
                Version = CurrentSettingsVersion,
                SteamApiKey = null,
                SteamId = null,
                CustomAnimationDllPath = null,
                WatchedFolders = defaultFolders.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };

            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (Settings != null)
                {
                    string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                    File.WriteAllText(SettingsFilePath, json);
                    Debug.WriteLine("Settings saved successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static TextBoxTraceListener GlobalTraceListener => _globalTraceListener;

        public void WatchSingleFile(Game game, string filePath)
        {
            _gameWatcherService?.StartWatchingSingleFile(game, filePath);
        }
        private async Task RunSetup(BootScreen bootScreen)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            bootScreen.Close();
            EventWaitHandle? setupCompleteEvent = null;
            try
            {
                if (!EventWaitHandle.TryOpenExisting(SetupCompleteEventName, out setupCompleteEvent))
                {
                    setupCompleteEvent = new EventWaitHandle(false, EventResetMode.ManualReset, SetupCompleteEventName);
                }
                else
                {
                    setupCompleteEvent.Reset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing setup event: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            try
            {
                SetupWindow setupWindow = new SetupWindow();
                setupWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start the setup process: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            bool signaled = false;
            await Task.Run(() =>
            {
                signaled = setupCompleteEvent.WaitOne(TimeSpan.FromMinutes(100));
            });
            setupCompleteEvent.Close();

            if (!signaled)
            {
                MessageBox.Show("Setup process did not complete in time. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            LoadUser();
            if (CurrentUser == null)
            {
                MessageBox.Show("Setup was not completed successfully or user data is missing. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
        }

        private async Task SetupLinkers(BootScreen bootScreen)
        {
            bootScreen.UpdateStatus("Setting up Steam integration...");

            if (Settings != null && !string.IsNullOrEmpty(Settings.SteamApiKey) && !string.IsNullOrEmpty(Settings.SteamId))
            {
                SteamWeb.InitializeSharedInstance(Settings.SteamApiKey, Settings.SteamId);
            }

            SteamScrape.Instance.ShowLoginWindow = () =>
            {
                var loginWindow = new SteamWebLoginWindow();
                if (loginWindow.ShowDialog() == true)
                {
                    return new Tuple<string, string>(loginWindow.SteamLoginSecure, loginWindow.SessionId);
                }
                return null;
            };

            var savedAccount = AccountManager.GetSavedAccounts().FirstOrDefault();
            if (savedAccount != null)
            {
                var session = AccountManager.GetSteamSession(savedAccount.Username);

                bootScreen.UpdateStatus($"Logging in as {savedAccount.Username}...");
                SteamScrape.Instance.SetSessionCookies(session.Item1, session.Item2, savedAccount.Username);

                bool sessionIsValid = await SteamScrape.Instance.TryRefreshSessionAsync();

                if (sessionIsValid)
                {
                    Debug.WriteLine($"Successfully logged in as {savedAccount.Username}.");
                    SetupSessionRefreshTimer();
                }
                else
                {
                    MessageBox.Show("Your saved Steam session has expired. Please log in again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                Debug.WriteLine("No saved Steam account found.");
            }
        }
        private void OnGameDataFound(Game foundGame)
        {
            Current.Dispatcher.Invoke(() =>
            {
                if (!(Current.MainWindow is MainWindow { IsLoaded: true } mainWindow))
                {
                    Trace.WriteLine("OnGameDataFound fired but MainWindow is not ready. Aborting.");
                    return;
                }

                var existingGame = CurrentUser.Games.FirstOrDefault(g => g.appid == foundGame.appid);
                if (existingGame == null)
                {
                    CurrentUser.Games.Add(foundGame);
                    existingGame = foundGame;
                    mainWindow.AddGame(foundGame);
                }

                bool changed = false;
                var existingAchievements = existingGame.achievements.ToDictionary(a => a.apiName);

                foreach (var newAchievement in foundGame.achievements)
                {
                    if (newAchievement.unlocked)
                    {
                        if (!existingAchievements.TryGetValue(newAchievement.apiName, out var existingAchievement) || !existingAchievement.unlocked)
                        {
                            Trace.WriteLine("ACHIEVEMENT FOUND");
                            AchievementWindow.ShowNotification(newAchievement, Settings.CustomAnimationDllPath);
                            changed = true;
                        }
                    }
                }

                existingGame.achievements = foundGame.achievements;

                if (changed)
                {
                    mainWindow.Save();
                }
            });
        }

        private void LoadUser()
        {
            if (File.Exists(UserFilePath))
            {
                try
                {
                    string json = File.ReadAllText(UserFilePath);

                    CurrentUser = JsonConvert.DeserializeObject<User>(json);

                    if (CurrentUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"User data loaded successfully for user: {CurrentUser.UserName}.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User data file exists but could not be deserialized. Resetting CurrentUser to null.");
                        CurrentUser = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading user data: {ex.Message}");
                    MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentUser = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User data file not found. Resetting CurrentUser to null.");
                CurrentUser = null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _gameWatcherService?.StopWatching();
            base.OnExit(e);
        }
    }
}