using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class GameShow : UserControl
    {
        private Game _game;
        private GameBanner _gameBannerFetcher;
        private Storyboard _sparkleStoryboard;

        public GameShow(Game game)
        {
            InitializeComponent();
            _game = game;
            _gameBannerFetcher = new GameBanner(_game.name);
            InitializeGameDataAsync();
            InitializeSparkleAnimation();
        }

        private async void InitializeGameDataAsync()
        {
            SetGameTitle(_game.name);
            string? imageUrl = await _gameBannerFetcher.GetGameBannerUrlAsync(_game.name);
            SetGameImage(imageUrl);

            int unlockedAchievements = _game.achievements?.Count(a => a.unlocked) ?? 0;
            int totalAchievements = _game.achievements?.Count ?? 0;
            SetAchievementInfo(unlockedAchievements, totalAchievements);
            UpdateAchievementBorder(unlockedAchievements, totalAchievements);
        }

        public void SetGameImage(string? imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                if (Uri.IsWellFormedUriString(imagePath, UriKind.Absolute))
                {
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    GameImage.Source = bitmap;
                }
                else
                {
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
                    Duration = TimeSpan.FromSeconds(5 + i * 0.2), // Vary speed
                    BeginTime = TimeSpan.FromSeconds(i * 0.5) // Stagger start times
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
            if (_sparkleStoryboard != null && (AchievementBorder.BorderBrush as SolidColorBrush)?.Color != Colors.Transparent)
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
    }
}