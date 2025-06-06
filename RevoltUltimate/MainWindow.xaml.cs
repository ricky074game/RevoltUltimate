using RevoltUltimate.Desktop.Pages;
using RevoltUltimate.Shared.Objects;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RevoltUltimate.Desktop
{
    public partial class MainWindow : Window
    {
        private User _currentUser = new("Player", 0, []);
        private const string UserFilePath = "user.json";
        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged(nameof(LevelDisplay));
                OnPropertyChanged(nameof(XpTooltip));
            }
        }

        public string LevelDisplay => CurrentUser?.GetLevel().ToString() ?? "1";
        public string XpTooltip => $"XP: {CurrentUser?.GetXpForCurrentLevel() ?? 0}/{CurrentUser?.GetXpForNextLevel() ?? 100}";

        public MainWindow()
        {
            InitializeComponent();
            LoadUser();
            UpdateXpBar();
            UpdateLevelAndTooltipText();
            AddGamesToGrid();
        }

        private void LoadUser()
        {
            if (File.Exists(UserFilePath))
            {
                string json = File.ReadAllText(UserFilePath);
                CurrentUser = JsonSerializer.Deserialize<User>(json) ?? new User("Player", 0, []);
            }
            else
            {
                CurrentUser = new User("Player", 0, []);
            }
            UpdateXpBar();
            UpdateLevelAndTooltipText();
        }

        private void SaveUser()
        {
            string json = JsonSerializer.Serialize(CurrentUser, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(UserFilePath, json);
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

            // If leveled up, animate to old max, then update max, reset, and animate to new value
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
                animation.Completed += (s, e) => completed();
            }
            XpProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void HamburgerMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings clicked.");
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("About clicked.");
        }
        private void AddGamesToGrid()
        {
            GamesGrid.Children.Clear();
            var game = new Game(
                "Age of Mythology",
                "PC",
                "",
                "A classic real-time strategy game.",
                "Standard"
            );
            var gameShow = new GameShow(game);
            GamesGrid.Children.Add(gameShow);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveUser();
            base.OnClosing(e);
        }
    }
}