using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RevoltUltimate.Desktop.Options
{
    public partial class UserProfileOptionsPage : UserControl
    {
        private string _originalUsername = string.Empty;
        private static readonly string ProfilePicturePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RevoltUltimate",
            "profile.jpg");

        public UserProfileOptionsPage()
        {
            InitializeComponent();
            Loaded += UserProfileOptionsPage_Loaded;
        }

        private void UserProfileOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProfileData();
            LoadStatistics();
        }

        private void LoadProfileData()
        {
            var user = App.CurrentUser;
            if (user == null) return;
            _originalUsername = user.UserName;
            UsernameTextBox.Text = _originalUsername;
            LevelText.Text = user.GetLevel().ToString();
            int currentXp = user.GetXpForCurrentLevel();
            int maxXp = user.GetXpForNextLevel();
            XpProgressBar.Maximum = maxXp;
            XpProgressBar.Value = currentXp;
            XpText.Text = $"{currentXp} / {maxXp} XP";
            TotalXpText.Text = $"Total XP: {user.Xp}";
            LoadProfilePicture();
        }

        private void LoadProfilePicture()
        {
            try
            {
                if (File.Exists(ProfilePicturePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(ProfilePicturePath);
                    bitmap.EndInit();
                    ProfileImageBrush.ImageSource = bitmap;
                }
                else
                {
                    var defaultBitmap = new BitmapImage(new Uri("pack://application:,,,/Images/profilePic.png"));
                    ProfileImageBrush.ImageSource = defaultBitmap;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error loading profile picture: {ex.Message}");
            }
        }

        private void LoadStatistics()
        {
            var user = App.CurrentUser;
            if (user == null) return;

            var allAchievements = user.Games.SelectMany(g => g.achievements).ToList();
            var unlockedAchievements = allAchievements.Where(a => a.unlocked).ToList();
            TotalGamesText.Text = user.Games.Count.ToString();
            TotalAchievementsText.Text = unlockedAchievements.Count.ToString();
            if (user.Games.Any(g => g.achievements.Any()))
            {
                var gamesWithAchievements = user.Games.Where(g => g.achievements.Any()).ToList();
                double avgCompletion = gamesWithAchievements.Average(g =>
                    (double)g.achievements.Count(a => a.unlocked) / g.achievements.Count * 100);
                CompletionRateText.Text = $"{avgCompletion:F1}%";
            }
            else
            {
                CompletionRateText.Text = "0%";
            }
            int perfectGames = user.Games.Count(g =>
                g.achievements.Any() && g.achievements.All(a => a.unlocked));
            PerfectGamesText.Text = perfectGames.ToString();
            LegendaryCount.Text = unlockedAchievements.Count(a => a.xp >= 200).ToString();
            HardCount.Text = unlockedAchievements.Count(a => a.xp is >= 90 and < 200).ToString();
            MediumCount.Text = unlockedAchievements.Count(a => a.xp is >= 50 and < 90).ToString();
            EasyCount.Text = unlockedAchievements.Count(a => a.xp is >= 30 and < 50).ToString();
            VeryEasyCount.Text = unlockedAchievements.Count(a => a.xp is > 0 and < 30).ToString();
            var platformStats = user.Games
                .GroupBy(g => g.platform ?? "Unknown")
                .Select(group => new PlatformStatistic
                {
                    Platform = group.Key,
                    GameCount = group.Count(),
                    TotalAchievements = group.Sum(g => g.achievements.Count),
                    UnlockedAchievements = group.Sum(g => g.achievements.Count(a => a.unlocked)),
                    CompletionPercent = group.Sum(g => g.achievements.Count) > 0
                        ? (int)((double)group.Sum(g => g.achievements.Count(a => a.unlocked)) /
                                group.Sum(g => g.achievements.Count) * 100)
                        : 0
                })
                .OrderByDescending(p => p.GameCount)
                .ToList();

            PlatformStatsItemsControl.ItemsSource = platformStats;
        }

        private void ProfilePicture_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Title = "Select Profile Picture"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string? directoryPath = Path.GetDirectoryName(ProfilePicturePath);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    File.Copy(openFileDialog.FileName, ProfilePicturePath, true);
                    LoadProfilePicture();

                    Trace.WriteLine($"Profile picture updated: {openFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error saving profile picture: {ex.Message}");
                    MessageBox.Show($"Failed to save profile picture: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasChanged = UsernameTextBox.Text != _originalUsername;
            SaveUsernameButton.Visibility = hasChanged ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveUsername_Click(object sender, RoutedEventArgs e)
        {
            var newUsername = UsernameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newUsername))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Text = _originalUsername;
                return;
            }

            if (App.CurrentUser != null)
            {
                App.CurrentUser.UserName = newUsername;
                _originalUsername = newUsername;
                SaveUsernameButton.Visibility = Visibility.Collapsed;
                SaveUserData();

                Trace.WriteLine($"Username changed to: {newUsername}");
            }
        }

        private void SaveUserData()
        {
            try
            {
                string userFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevoltUltimate",
                    "user.json");

                string? directoryPath = Path.GetDirectoryName(userFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (App.CurrentUser != null)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(App.CurrentUser,
                        Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(userFilePath, json);
                    Trace.WriteLine("User data saved successfully.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving user data: {ex.Message}");
                MessageBox.Show($"Failed to save user data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class PlatformStatistic
    {
        public string Platform { get; set; } = string.Empty;
        public int GameCount { get; set; }
        public int TotalAchievements { get; set; }
        public int UnlockedAchievements { get; set; }
        public int CompletionPercent { get; set; }
    }
}