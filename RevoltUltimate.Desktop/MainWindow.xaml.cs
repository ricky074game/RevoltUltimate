using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Notification;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Pages;
using RevoltUltimate.Desktop.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RevoltUltimate.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly String _userFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "user.json");
        private User CurrentUser => App.CurrentUser;
        private readonly String _settingsFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");
        private ConsoleWindow _consoleWindow;

        private TaskbarIcon _taskbarIcon;
        public string ProfilePicturePath { get; set; }
        private bool _isExplicitlyClosing = false;
        private DispatcherTimer _backgroundTaskTimer;
        private bool _isBackgroundTaskRunning = false;
        private UniformGrid _gamesGrid = new() { Columns = 5, Margin = new Thickness(15) };


        public NotificationViewModel NotificationViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            String profilePicturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "profile.jpg");
            ProfilePicturePath = File.Exists(profilePicturePath) ? profilePicturePath : "Images/profilePic.png";
            ConsoleMenuItem.Visibility = App.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
            ConsoleCometMenuItem.Visibility = App.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
            DataContext = this;
            NotificationViewModel = new NotificationViewModel();
            NotificationSystem.DataContext = NotificationViewModel;
            UpdateXpBar();
            UpdateLevelAndTooltipText();
            AddGamesToGrid(true);

            InitializeTaskbarIcon();
            InitializeBackgroundTask();
        }

        private void InitializeTaskbarIcon()
        {
            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.ToolTipText = "RevoltUltimate";

            try
            {
                var iconUri = new Uri("pack://application:,,,/images/RoninLogo.ico");
                BitmapImage bitmap = new BitmapImage(iconUri);
                _taskbarIcon.IconSource = new BitmapImage(iconUri);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error loading icon for TaskbarIcon: {ex.Message}");
            }

            var contextMenu = new ContextMenu();
            if (FindResource("SharedContextMenuStyle") is Style contextMenuStyle)
            {
                contextMenu.Style = contextMenuStyle;
            }

            var openItem = new MenuItem { Header = "Open" };
            if (FindResource("SharedMenuItemStyle") is Style menuItemStyle)
            {
                openItem.Style = menuItemStyle;
            }
            openItem.Click += TaskbarIcon_Open_Click;
            contextMenu.Items.Add(openItem);

            var closeItem = new MenuItem { Header = "Close" };
            if (FindResource("SharedMenuItemStyle") is Style menuItemStyleForClose)
            {
                closeItem.Style = menuItemStyleForClose;
            }
            closeItem.Click += TaskbarIcon_Close_Click;
            contextMenu.Items.Add(closeItem);

            _taskbarIcon.ContextMenu = contextMenu;
            _taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick_Open;
        }

        private void TaskbarIcon_TrayMouseDoubleClick_Open(object sender, RoutedEventArgs e)
        {
            PerformOpenAction();
        }

        private void TaskbarIcon_Open_Click(object sender, RoutedEventArgs e)
        {
            PerformOpenAction();
        }

        private void TaskbarIcon_Close_Click(object sender, RoutedEventArgs e)
        {
            PerformCloseAction();
        }

        private void PerformOpenAction()
        {
            this.Show();
            WindowState = WindowState.Normal;
            this.Activate();
            ShowInTaskbar = true;
        }
        private void Console_Click(object sender, RoutedEventArgs e)
        {
            if (!_consoleWindow.IsLoaded)
            {
                _consoleWindow = new ConsoleWindow
                {
                    Owner = this
                };
                _consoleWindow.Show();
            }
            else
            {
                _consoleWindow.Activate();
            }
        }

        private void CometConsole_Click(object sender, RoutedEventArgs e)
        {
            if (App.CometConsoleWindow != null)
            {
                if (App.CometConsoleWindow.IsVisible)
                {
                    App.CometConsoleWindow.Activate();
                }
                else
                {
                    App.CometConsoleWindow.Show();
                }
            }
        }
        private void PerformCloseAction()
        {
            _isExplicitlyClosing = true;
            _backgroundTaskTimer?.Stop();
            _taskbarIcon?.Dispose();
            _taskbarIcon = null;
            Application.Current.Shutdown();
        }

        private void InitializeBackgroundTask()
        {
            _backgroundTaskTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _backgroundTaskTimer.Tick += BackgroundTask_Tick;
            _backgroundTaskTimer.Start();
        }

        private void BackgroundTask_Tick(object sender, EventArgs e)
        {
            RunPeriodicBackgroundTask();
        }

        public async void RunPeriodicBackgroundTask()
        {
            if (_isBackgroundTaskRunning)
            {
                Trace.WriteLine("Background task is already running. Skipping new invocation.");
                return;
            }

            _isBackgroundTaskRunning = true;
            Trace.WriteLine($"Background task running at: {DateTime.Now}");
            string taskName = "Checking for new achievements";
            NotificationViewModel.AddTask(taskName);
            var savedAccount = AccountManager.GetSavedAccounts().FirstOrDefault();

            try
            {
                var steamTask = Task.Run(async () =>
                {
                    var games = new List<Game>();
                    if (savedAccount != null)
                    {
                        var steamSessionCookies = AccountManager.GetSteamSession(savedAccount.Username);
                        if (steamSessionCookies != null && steamSessionCookies.Any())
                        {
                            SteamScrape.Instance.SetSessionCookies(steamSessionCookies, savedAccount.Username);
                            games = await SteamScrape.Instance.GetOwnedGamesAsync();
                        }
                    }
                    return games;
                });

                var gogTask = Task.Run(async () =>
                {
                    var games = new List<Game>();
                    if (savedAccount != null)
                    {
                        var gogTokens = AccountManager.GetGOGTokens(savedAccount.Username);
                        if (!string.IsNullOrEmpty(gogTokens.AccessToken))
                        {
                            games = await GOG.Instance.GetOwnedGamesWithAchievementsAsync();
                        }
                    }
                    return games;
                });
                await Task.WhenAll(steamTask, gogTask);
                var allLiveGames = (await steamTask).Concat(await gogTask).ToList();
                bool foundAny = false;
                var processingTasks = new List<Task>();
                foreach (var liveGame in allLiveGames)
                {
                    var localGame = CurrentUser.Games.FirstOrDefault(g => g.appid == liveGame.appid && g.platform == liveGame.platform);
                    if (localGame != null)
                    {
                        processingTasks.Add(Task.Run(() =>
                        {
                            var liveAchievements = liveGame.achievements
                                .GroupBy(a => a.id)
                                .ToDictionary(g => g.Key, g => g.First());

                            foreach (var localAch in localGame.achievements)
                            {
                                if (!localAch.unlocked && liveAchievements.TryGetValue(localAch.id, out var liveAch) && liveAch.unlocked)
                                {
                                    localAch.SetUnlockedStatus(true, liveAch.datetimeunlocked);
                                    Dispatcher.Invoke(() =>
                                    {
                                        AchievementWindow.ShowNotification(localAch, App.Settings?.CustomAnimationDllPath);
                                        AddXp(localAch.xp);
                                    });
                                    foundAny = true;
                                }
                            }
                        }));
                    }
                }
                await Task.WhenAll(processingTasks);
                if (foundAny)
                {
                    Save();
                }
                NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Success);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error checking for achievements: {ex.Message}");
                NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Failed, ex.Message);
            }
            finally
            {
                _isBackgroundTaskRunning = false;
            }
        }

        public void Save()
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(_userFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (CurrentUser != null)
                {
                    string userJson = JsonConvert.SerializeObject(CurrentUser, Formatting.Indented);
                    File.WriteAllText(_userFilePath, userJson);
                }

                if (App.Settings != null)
                {
                    string settingsJson = JsonConvert.SerializeObject(App.Settings, Formatting.Indented);
                    File.WriteAllText(_settingsFilePath, settingsJson);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
                return;

            int prevXp = CurrentUser.GetXpForCurrentLevel();
            int prevMaxXp = CurrentUser.GetXpForNextLevel();

            CurrentUser.Xp += amount;
            Save();

            int newXp = CurrentUser.GetXpForCurrentLevel();
            int newMaxXp = CurrentUser.GetXpForNextLevel();

            void UpdateUi()
            {
                if (newXp < prevXp)
                {
                    AnimateXpBar(prevXp, prevMaxXp, () =>
                    {
                        XpProgressBar.Maximum = newMaxXp;
                        XpProgressBar.Value = 0;
                        AnimateXpBar(0, newXp);
                        UpdateLevelAndTooltipText();
                    });
                }
                else
                {
                    XpProgressBar.Maximum = newMaxXp;
                    AnimateXpBar(prevXp, newXp);
                    UpdateLevelAndTooltipText();
                }
            }

            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(UpdateUi);
            else
                UpdateUi();
        }
        private void UpdateXpBar()
        {
            int currentXp = CurrentUser.GetXpForCurrentLevel();
            int maxXp = CurrentUser.GetXpForNextLevel();
            XpProgressBar.Maximum = maxXp;
            XpProgressBar.Value = currentXp;
            UpdateLevelAndTooltipText();
        }
        private void UpdateLevelAndTooltipText()
        {
            LevelTextBlock.Text = CurrentUser.GetLevel().ToString();
            XpBarGrid.ToolTip = $"XP: {CurrentUser.GetXpForCurrentLevel()}/{CurrentUser.GetXpForNextLevel()}";
        }

        private void AnimateXpBar(double fromValue, double toValue, Action? completed = null, double durationSeconds = 0.7)
        {
            var animation = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            if (completed != null)
            {
                animation.Completed += (s, e_anim) => completed();
            }
            XpProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            ShowInTaskbar = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            base.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void HamburgerMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow optionsWindow = new OptionsWindow
            {
                Owner = this
            };
            optionsWindow.ShowDialog();
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new GameSearchWindow();
            searchWindow.ShowDialog();
        }

        private async Task AddGamesToGrid(bool load)
        {
            MainContentControl.Content = new ScrollViewer
            {
                Content = _gamesGrid,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _gamesGrid.Children.Clear();
            AddSelectGamesToGrid(CurrentUser.Games, false);

            if (load)
            {
                var allFetchedGames = new List<Game>();
                if (App.Settings != null && !string.IsNullOrEmpty(App.Settings.SteamApiKey) && !string.IsNullOrWhiteSpace(App.Settings.SteamId))
                {
                    NotificationViewModel.AddTask("Fetching games from Steam API");
                    var steamApiGames = await SteamWeb.Instance.Update();
                    allFetchedGames.AddRange(steamApiGames);
                    NotificationViewModel.UpdateTaskStatus("Fetching games from Steam API", NotificationStatus.Success);
                }

                NotificationViewModel.AddTask("Fetching games from Steam profile");
                var steamScrapeGames = await SteamScrape.Instance.GetOwnedGamesAsync();
                allFetchedGames.AddRange(steamScrapeGames);
                NotificationViewModel.UpdateTaskStatus("Fetching games from Steam profile", NotificationStatus.Success);
                NotificationViewModel.AddTask("Fetching games from GOG");
                var gogGames = await GOG.Instance.GetOwnedGamesWithAchievementsAsync();
                allFetchedGames.AddRange(gogGames);
                NotificationViewModel.UpdateTaskStatus("Fetching games from GOG", NotificationStatus.Success);

                var newGamesFound = new List<Game>();
                foreach (var fetchedGame in allFetchedGames)
                {
                    var existingGame = CurrentUser.Games.FirstOrDefault(g => g.appid == fetchedGame.appid && g.platform == fetchedGame.platform);
                    if (existingGame == null)
                    {
                        CurrentUser.Games.Add(fetchedGame);
                        newGamesFound.Add(fetchedGame);
                    }
                    else
                    {
                        existingGame.name = fetchedGame.name;
                        existingGame.imageUrl = fetchedGame.imageUrl;
                    }
                }

                if (newGamesFound.Any())
                {
                    AddSelectGamesToGrid(newGamesFound, true);
                }
            }

            Save();
        }
        public void AddGame(Game game)
        {
            if (CurrentUser.Games.Any(g => g.name.Equals(game.name, StringComparison.OrdinalIgnoreCase)))
            {
                var existingGame = CurrentUser.Games.First(g => g.name.Equals(game.name, StringComparison.OrdinalIgnoreCase));
                existingGame.achievements = game.achievements;
            }
            else
            {
                CurrentUser.Games.Add(game);
            }

            _gamesGrid.Children.Clear();
            AddSelectGamesToGrid(CurrentUser.Games, true);
            Save();
        }

        private void AddSelectGamesToGrid(IEnumerable<Game> games, bool achievementXP)
        {
            if (!games.Any())
            {
                Trace.WriteLine("No games to add to the grid.");
                Save();
                return;
            }
            foreach (var game in games)
            {
                if (_gamesGrid.Children.OfType<GameShow>().Any(g => g.DataContext is Game existingGame
                                                                    && existingGame.name.Equals(game.name, StringComparison.OrdinalIgnoreCase)
                                                                    && existingGame.platform.Equals(game.platform, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var gameShowControl = new GameShow(game);
                gameShowControl.GameClicked += OnGameClicked;
                _gamesGrid.Children.Add(gameShowControl);
                if (achievementXP)
                {
                    //Add XP for every achievement that is already unlocked based on the achievement's xp
                    foreach (var achievement in game.achievements)
                    {
                        if (achievement.unlocked)
                        {
                            AddXp(achievement.xp);
                        }
                    }
                }
            }
            Save();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Save();

            if (!_isExplicitlyClosing)
            {
                // Check if MinimizeToTray is enabled
                if (App.Settings?.MinimizeToTray == true)
                {
                    e.Cancel = true;
                    this.Hide();
                    ShowInTaskbar = false;
                }
                else
                {
                    // Close the application completely
                    _isExplicitlyClosing = true;
                    _backgroundTaskTimer?.Stop();
                    _taskbarIcon?.Dispose();
                    _taskbarIcon = null;
                    Application.Current.Shutdown();
                }
            }
            else
            {
                _backgroundTaskTimer?.Stop();
                _taskbarIcon?.Dispose();
                _taskbarIcon = null;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _backgroundTaskTimer?.Stop();
            _taskbarIcon?.Dispose();
            _taskbarIcon = null;
            base.OnClosed(e);
        }
        private void OnGameClicked(object sender, Game game)
        {
            var achievementsPage = new GameAchievementsPage(game);
            achievementsPage.BackClicked += OnBackClicked;

            MainContentControl.Content = achievementsPage;
        }

        private void OnBackClicked(object sender, System.EventArgs e)
        {
            AddGamesToGrid(false);
        }

        private void ReloadOnClick(object sender, RoutedEventArgs e)
        {
            AddGamesToGrid(false);
        }
    }
}