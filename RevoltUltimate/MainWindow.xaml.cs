using Hardcodet.Wpf.TaskbarNotification;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Pages;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
        private TaskbarIcon _taskbarIcon;
        private bool _isExplicitlyClosing = false;
        private DispatcherTimer _backgroundTaskTimer;

        public MainWindow()
        {
            InitializeComponent();
            UpdateXpBar();
            UpdateLevelAndTooltipText();
            AddGamesToGrid();

            InitializeTaskbarIcon();
            InitializeBackgroundTask();
        }

        private void InitializeTaskbarIcon()
        {
            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.ToolTipText = "RevoltUltimate";

            try
            {
                var iconUri = new Uri("pack://application:,,,/images/RoninLogo.png");
                _taskbarIcon.IconSource = new BitmapImage(iconUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon for TaskbarIcon: {ex.Message}");
            }

            // Create WPF ContextMenu and apply style
            var contextMenu = new ContextMenu();
            if (FindResource("SharedContextMenuStyle") is Style contextMenuStyle)
            {
                contextMenu.Style = contextMenuStyle;
            }

            // Create MenuItems and apply style
            var openItem = new MenuItem { Header = "Open" };
            if (FindResource("SharedMenuItemStyle") is Style menuItemStyle) // Use FindResource
            {
                openItem.Style = menuItemStyle;
            }
            openItem.Click += TaskbarIcon_Open_Click;
            contextMenu.Items.Add(openItem);

            var closeItem = new MenuItem { Header = "Close" };
            if (FindResource("SharedMenuItemStyle") is Style menuItemStyleForClose) // Use FindResource
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
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.ShowInTaskbar = true;
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
            RunPeriodicBackgroundTask();
        }

        private void BackgroundTask_Tick(object sender, EventArgs e)
        {
            RunPeriodicBackgroundTask();
        }

        public void RunPeriodicBackgroundTask()
        {
            System.Diagnostics.Debug.WriteLine($"Background task running at: {DateTime.Now}");
        }

        private void SaveUser()
        {
            string json = JsonSerializer.Serialize(CurrentUser, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_userFilePath, json);
        }

        private void UpdateXpBar()
        {
            int currentXp = CurrentUser.GetXpForCurrentLevel();
            int maxXp = CurrentUser.GetXpForNextLevel();
            System.Diagnostics.Debug.WriteLine(maxXp);
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
            SaveUser();
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
            this.ShowInTaskbar = false;
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
            System.Windows.MessageBox.Show("About clicked.");
        }

        private async void AddGamesToGrid()
        {
            if (CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("CurrentUser is null. Cannot fetch games.");
                return;
            }

            if (!MainSteam.Instance.IsSteamApiReady)
            {
                System.Diagnostics.Debug.WriteLine("Steam Web API is not ready (check API key and Steam ID). Cannot fetch games.");
                return;
            }

            try
            {
                List<Game> steamGames = await MainSteam.Instance.Update();

                if (CurrentUser.Games == null)
                {
                    CurrentUser.Games = new List<Game>();
                }

                bool newGamesAdded = false;
                if (steamGames != null) // Good practice to check if the list itself could be null
                {
                    foreach (var steamGame in steamGames)
                    {
                        bool gameExists = CurrentUser.Games.Any(g =>
                            g.Name.Equals(steamGame.Name, StringComparison.OrdinalIgnoreCase) &&
                            g.Platform.Equals(steamGame.Platform, StringComparison.OrdinalIgnoreCase));

                        if (!gameExists)
                        {
                            CurrentUser.Games.Add(steamGame);
                            newGamesAdded = true;
                            System.Diagnostics.Debug.WriteLine($"Added game to CurrentUser.Games: {steamGame.Name} ({steamGame.Platform})");
                        }
                    }
                }

                if (newGamesAdded)
                {
                    SaveUser(); // Save if new games were added to the persistent list
                }

                GamesGrid.Children.Clear(); // Clear existing games to avoid duplicates in the UI
                if (CurrentUser.Games != null)
                {
                    foreach (var game in CurrentUser.Games)
                    {
                        var gameShowControl = new GameShow(game);
                        GamesGrid.Children.Add(gameShowControl);
                    }
                    System.Diagnostics.Debug.WriteLine($"GamesGrid updated. Total games in UI: {CurrentUser.Games.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CurrentUser.Games is null after processing. No games to show.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddGamesToGrid: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveUser();

            if (!_isExplicitlyClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
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
    }
}