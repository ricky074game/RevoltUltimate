using Microsoft.Win32;
using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using RevoltUltimate.Desktop.Setup;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class GameShow : UserControl
    {
        private Game _game;
        private GameBanner _gameBannerFetcher;
        private Storyboard _sparkleStoryboard;

        public event EventHandler<Game> GameClicked;

        public bool IsSteamPirated => _game?.method?.Contains("Emulator") == true;

        public GameShow(Game game)
        {
            InitializeComponent();
            _game = game;
            _gameBannerFetcher = new GameBanner(_game.name);
            DataContext = this; // Set DataContext for binding
            InitializeGameDataAsync();
            InitializeSparkleAnimation();
        }

        private async void InitializeGameDataAsync()
        {
            SetGameTitle(_game.name);
            string? imageUrl = await _gameBannerFetcher.GetBannerImagePathAsync(_game.name);
            SetGameImage(imageUrl);

            int unlockedAchievements = _game.achievements?.Count(a => a.unlocked) ?? 0;
            int totalAchievements = _game.achievements?.Count ?? 0;
            SetAchievementInfo(unlockedAchievements, totalAchievements);
            UpdateAchievementBorder(unlockedAchievements, totalAchievements);
        }

        private void SetGameImage(string? imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    GameImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                    GameImage.Source = null;
                }
            }
            else
            {
                GameImage.Source = null;
            }
        }

        public void SetGameTitle(string title)
        {
            GameTitleText.Text = title;
        }

        public void SetAchievementInfo(int unlocked, int total)
        {
            string percentText = total > 0 ? $"{(unlocked * 100 / total)}%" : "0%";
            AchievementInfoText.Text = $"Achievements: {unlocked}/{total}, {percentText}";
        }
        private void OnGameAchievementsLoaded(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateAchievementDisplay);
        }

        private void UpdateAchievementDisplay()
        {
            if (_game == null) return;

            int unlockedAchievements = _game.achievements?.Count(a => a.unlocked) ?? 0;
            int totalAchievements = _game.achievements?.Count ?? 0;
            SetAchievementInfo(unlockedAchievements, totalAchievements);
            UpdateAchievementBorder(unlockedAchievements, totalAchievements);
        }

        private void UpdateAchievementBorder(int unlocked, int total)
        {
            double percentage = 0;
            if (total > 0)
            {
                percentage = (double)unlocked / total * 100;
            }

            SolidColorBrush borderBrush = new SolidColorBrush(Colors.Transparent);
            Color glowColor = Colors.Transparent;

            if (percentage >= 100)
            {
                borderBrush = new SolidColorBrush(Colors.Aqua); // Diamond
                glowColor = Colors.Aqua;
                StartSparkleAnimation();
            }
            else if (percentage >= 90)
            {
                borderBrush = new SolidColorBrush(Colors.Gold); // Gold
                glowColor = Colors.Gold;
                StartSparkleAnimation();
            }
            else if (percentage >= 75)
            {
                borderBrush = new SolidColorBrush(Colors.Silver); // Silver
                glowColor = Colors.Silver;
                StopSparkleAnimation();
            }
            else if (percentage >= 50)
            {
                borderBrush = new SolidColorBrush(Color.FromRgb(205, 127, 50)); // Bronze
                glowColor = Color.FromRgb(205, 127, 50);
                StopSparkleAnimation();
            }
            else
            {
                StopSparkleAnimation();
            }

            AchievementBorder.BorderBrush = borderBrush;
            BorderGlowEffect.Color = glowColor;
            BorderGlowEffect.Opacity = (glowColor == Colors.Transparent) ? 0 : 0.75;
        }

        private void InitializeSparkleAnimation()
        {
            _sparkleStoryboard = new Storyboard();
            _sparkleStoryboard.RepeatBehavior = RepeatBehavior.Forever;

            for (int i = 0; i < 10; i++)
            {
                var sparkle = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.White,
                    Opacity = 0
                };
                SparkleCanvas.Children.Add(sparkle);

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure { IsClosed = true, StartPoint = GetPointOnBorder(0) };
                pathFigure.Segments.Add(new PolyLineSegment(new Point[] {
                    GetPointOnBorder(0.25), GetPointOnBorder(0.5), GetPointOnBorder(0.75), GetPointOnBorder(1)
                }, true));
                pathGeometry.Figures.Add(pathFigure);


                var animationX = new DoubleAnimationUsingPath
                {
                    PathGeometry = pathGeometry,
                    Source = PathAnimationSource.X,
                    Duration = TimeSpan.FromSeconds(5 + i * 0.2),
                    BeginTime = TimeSpan.FromSeconds(i * 0.5)
                };
                Storyboard.SetTarget(animationX, sparkle);
                Storyboard.SetTargetProperty(animationX, new PropertyPath("(Canvas.Left)"));
                _sparkleStoryboard.Children.Add(animationX);

                var animationY = new DoubleAnimationUsingPath
                {
                    PathGeometry = pathGeometry,
                    Source = PathAnimationSource.Y,
                    Duration = TimeSpan.FromSeconds(5 + i * 0.2),
                    BeginTime = TimeSpan.FromSeconds(i * 0.5)
                };
                Storyboard.SetTarget(animationY, sparkle);
                Storyboard.SetTargetProperty(animationY, new PropertyPath("(Canvas.Top)"));
                _sparkleStoryboard.Children.Add(animationY);

                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    AutoReverse = true,
                    Duration = TimeSpan.FromSeconds(0.5),
                    BeginTime = TimeSpan.FromSeconds(i * 0.5)
                };
                Storyboard.SetTarget(opacityAnimation, sparkle);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Ellipse.OpacityProperty));
                _sparkleStoryboard.Children.Add(opacityAnimation);
            }
        }

        private Point GetPointOnBorder(double progress)
        {
            double width = AchievementBorder.ActualWidth > 0 ? AchievementBorder.ActualWidth : 160; // d:DesignWidth as fallback
            double height = AchievementBorder.ActualHeight > 0 ? AchievementBorder.ActualHeight : 250; // d:DesignHeight as fallback
            double perimeter = 2 * (width + height);
            double distance = progress * perimeter;

            if (distance < width) return new Point(distance - 2, -2); // Top edge
            distance -= width;
            if (distance < height) return new Point(width - 2, distance - 2); // Right edge
            distance -= height;
            if (distance < width) return new Point(width - distance - 2, height - 2); // Bottom edge
            distance -= width;
            return new Point(-2, height - distance - 2); // Left edge
        }


        private void StartSparkleAnimation()
        {
            if ((AchievementBorder.BorderBrush as SolidColorBrush)?.Color != Colors.Transparent)
            {
                // Update sparkle colors if needed, e.g., to match border
                foreach (var child in SparkleCanvas.Children)
                {
                    if (child is Ellipse sparkle)
                    {
                        sparkle.Fill = AchievementBorder.BorderBrush;
                    }
                }
                _sparkleStoryboard.Begin();
            }
        }

        private void StopSparkleAnimation()
        {
            _sparkleStoryboard?.Stop();
            SparkleCanvas.Children.Clear(); // Clear old sparkles if any
            InitializeSparkleAnimation(); // Re-add sparkles so they are ready
        }
        private void GameShow_Unloaded(object sender, RoutedEventArgs e)
        {
            _sparkleStoryboard?.Stop(this);
        }

        private void GameShow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            GameClicked?.Invoke(this, _game);
        }

        private void SetBanner_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "Select a Game Banner"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _game.imageUrl = openFileDialog.FileName;
                SetGameImage(_game.imageUrl);
                // Consider saving the game state here
            }
        }

        private void ChangeTitle_Click(object sender, RoutedEventArgs e)
        {
            var fields = new List<InputFieldDefinition>
            {
                new InputFieldDefinition { Label = "New Title", DefaultValue = _game.name }
            };
            var dialog = new InputDialog("Enter new title:", fields, null)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.Responses.TryGetValue("New Title", out var newTitle))
            {
                _game.name = newTitle;
                SetGameTitle(_game.name);
                // Consider saving the game state here
            }
        }

        private void ChangeDescription_Click(object sender, RoutedEventArgs e)
        {
            var fields = new List<InputFieldDefinition>
            {
                new InputFieldDefinition { Label = "New Description", DefaultValue = _game.description }
            };
            var dialog = new InputDialog("Enter new description:", fields, null)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.Responses.TryGetValue("New Description", out var newDescription))
            {
                _game.description = newDescription;
            }
        }

        private void GenerateJson_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is not yet implemented.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SelectJson_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files|*.json",
                Title = "Select achievements.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    var steamAchievements = JsonSerializer.Deserialize<List<SteamAchievement>>(jsonContent);

                    if (steamAchievements != null)
                    {
                        var achievements = steamAchievements.Select(sa => new Achievement(
                            Name: sa.DisplayName,
                            Description: sa.Description,
                            ImageUrl: Path.Combine(System.IO.Path.GetDirectoryName(openFileDialog.FileName) ?? string.Empty, sa.Icon),
                            Hidden: sa.Hidden == 1,
                            Id: 0,
                            Unlocked: false,
                            DateTimeUnlocked: null,
                            Difficulty: 0,
                            apiName: sa.Name,
                            progress: false,
                            currentProgress: 0,
                            maxProgress: 0,
                            getglobalpercentage: 0.0f
                        )).ToList();

                        _game.achievements = achievements;
                        UpdateAchievementDisplay();
                        MessageBox.Show("Achievements updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectTrackFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is not yet implemented.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}