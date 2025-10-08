using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Notification;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.API.Update;
using RevoltUltimate.Desktop.Pages;
using RevoltUltimate.Desktop.Windows;
using System.IO;
using System.Net.Http;
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
        private UniformGrid _gamesGrid = new UniformGrid { Columns = 5, Margin = new Thickness(15) };


        public NotificationViewModel NotificationViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            String profilePicturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "profile.jpg");
            if (File.Exists(profilePicturePath))
            {
                ProfilePicturePath = profilePicturePath;
            }
            else
            {
                ProfilePicturePath = "Images/profilePic.png";
            }
            ConsoleMenuItem.Visibility = App.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
            ConsoleCometMenuItem.Visibility = App.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
            DataContext = this;
            NotificationViewModel = new NotificationViewModel();
            NotificationSystem.DataContext = NotificationViewModel;
            UpdateXpBar();
            UpdateLevelAndTooltipText();
            AddGamesToGrid(false, true);

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
                System.Diagnostics.Debug.WriteLine($"Error loading icon for TaskbarIcon: {ex.Message}");
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
            if (_consoleWindow == null || !_consoleWindow.IsLoaded)
            {
                _consoleWindow = new ConsoleWindow();
                _consoleWindow.Owner = this;
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
            System.Windows.Application.Current.Shutdown();
        }

        private void InitializeBackgroundTask()
        {
            _backgroundTaskTimer = new DispatcherTimer();
            _backgroundTaskTimer.Interval = TimeSpan.FromSeconds(30);
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
                System.Diagnostics.Debug.WriteLine("Background task is already running. Skipping new invocation.");
                return;
            }

            _isBackgroundTaskRunning = true;
            System.Diagnostics.Debug.WriteLine($"Background task running at: {DateTime.Now}");
            string taskName = "Checking for new achievements";
            NotificationViewModel.AddTask(taskName);

            try
            {
                var achievementTasks = CurrentUser.Games
                    .Select(game =>
                    {
                        Update updater = null;
                        if (game.method.Equals("Steam Local", StringComparison.OrdinalIgnoreCase))
                        {
                            updater = new SteamLocalUpdate();
                        }
                        else if (game.method.Equals("Steam Web API", StringComparison.OrdinalIgnoreCase))
                        {
                            updater = new SteamUpdate();
                        }

                        return updater != null
                            ? updater.CheckForNewAchievementsAsync(game)
                            : Task.FromResult<List<Achievement>>(new List<Achievement>());
                    })
                    .ToList();

                var allNewAchievements = await Task.WhenAll(achievementTasks);

                bool foundAny = false;
                for (int i = 0; i < CurrentUser.Games.Count; i++)
                {
                    var newAchievements = allNewAchievements[i];
                    if (newAchievements.Any())
                    {
                        foundAny = true;
                        System.Diagnostics.Debug.WriteLine("FOUND NEW ACHIEVEMENT!!");
                        foreach (var achievement in newAchievements)
                        {
                            AchievementWindow.ShowNotification(achievement, App.Settings.CustomAnimationDllPath);
                        }
                    }
                }
                if (foundAny)
                {
                    Save();
                }
                NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Success);
                Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for achievements: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex.Message}");
            }
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
        private void AddXpButton_Click(object sender, RoutedEventArgs e)
        {
            int prevXp = CurrentUser.GetXpForCurrentLevel();
            int prevMaxXp = CurrentUser.GetXpForNextLevel();
            CurrentUser.Xp += 10;
            Save();
            int newXp = CurrentUser.GetXpForCurrentLevel();
            int newMaxXp = CurrentUser.GetXpForNextLevel();

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

        private async Task AddGamesToGrid(bool back, bool load)
        {
            var allGames = new List<Game>();

            MainContentControl.Content = new ScrollViewer
            {
                Content = _gamesGrid,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            if (!back)
            {
                if (load)
                {
                    if (CurrentUser.Games.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Loaded {CurrentUser.Games.Count} local games.");
                        AddSelectGamesToGrid(CurrentUser.Games);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No local games found in CurrentUser.Games.");
                    }
                }
                if (App.Settings != null && !string.IsNullOrEmpty(App.Settings.SteamApiKey) &&
                !string.IsNullOrWhiteSpace(App.Settings.SteamId))
                {
                    if (SteamWeb.Instance.IsSteamApiReady)
                    {
                        string taskName = "Fetching games from Steam API";
                        NotificationViewModel.AddTask(taskName);

                        try
                        {
                            List<Game> steamGames = await SteamWeb.Instance.Update();
                            if (steamGames != null)
                            {
                                foreach (var steamGame in steamGames)
                                {
                                    bool gameExists = allGames.Any(g =>
                                        g.name.Equals(steamGame.name, StringComparison.OrdinalIgnoreCase) &&
                                        g.platform.Equals(steamGame.platform, StringComparison.OrdinalIgnoreCase));

                                    if (!gameExists)
                                    {
                                        allGames.Add(steamGame);
                                        System.Diagnostics.Debug.WriteLine(
                                            $"Added game from Steam API: {steamGame.name} ({steamGame.platform})");
                                    }
                                }
                            }

                            NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Success);
                        }
                        catch (HttpRequestException httpEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error fetching games from Steam API: {httpEx.Message}");
                            NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Failed, httpEx.Message);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in Steam API fetch: {ex.Message}");
                            NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Failed,
                                "An unexpected error occurred.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Steam Web API is not ready (check API key and Steam ID).");
                    }
                }

                if (SteamScrape.Instance != null)
                {
                    string taskName = "Fetching games from Steam Local";
                    NotificationViewModel.AddTask(taskName);

                    try
                    {
                        List<Game> localSteamGames = await SteamScrape.Instance.GetOwnedGamesAsync();
                        foreach (var localSteamGame in localSteamGames)
                        {
                            bool gameExists = allGames.Any(g =>
                                g.name.Equals(localSteamGame.name, StringComparison.OrdinalIgnoreCase) &&
                                g.platform.Equals(localSteamGame.platform, StringComparison.OrdinalIgnoreCase));

                            if (!gameExists)
                            {
                                allGames.Add(localSteamGame);
                                System.Diagnostics.Debug.WriteLine($"Added game from Steam Local: {localSteamGame.name} ({localSteamGame.platform})");
                            }
                        }
                        NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Success);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error fetching games from Steam Local: {ex.Message}");
                        NotificationViewModel.UpdateTaskStatus(taskName, NotificationStatus.Failed, "An unexpected error occurred.");
                    }
                }

                foreach (var game in allGames)
                {
                    bool gameExistsInCurrentUser = CurrentUser.Games.Any(g =>
                        g.name.Equals(game.name, StringComparison.OrdinalIgnoreCase) &&
                        g.platform.Equals(game.platform, StringComparison.OrdinalIgnoreCase));

                    if (!gameExistsInCurrentUser)
                    {
                        CurrentUser.Games.Add(game);
                    }
                }

                AddSelectGamesToGrid(CurrentUser.Games);
            }
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
            AddSelectGamesToGrid(CurrentUser.Games);
            Save();
        }

        private void AddSelectGamesToGrid(IEnumerable<Game> games)
        {
            if (!games.Any())
            {
                System.Diagnostics.Debug.WriteLine("No games to add to the grid.");
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
            }
            Save();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Save();

            if (!_isExplicitlyClosing)
            {
                e.Cancel = true;
                this.Hide();
                ShowInTaskbar = false;
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
            AddGamesToGrid(true, false);
        }

        private void ReloadOnClick(object sender, RoutedEventArgs e)
        {
            AddGamesToGrid(false, false);
        }
    }
}