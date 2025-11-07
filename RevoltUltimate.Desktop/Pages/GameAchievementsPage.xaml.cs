using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class GameAchievementsPage : UserControl
    {
        public event EventHandler BackClicked;
        private readonly AchievementImage _achievementImageFetcher;
        private class AchievementDisplayData
        {
            public Achievement OriginalAchievement { get; set; }
            public BitmapImage ProcessedImage { get; set; }
        }

        public GameAchievementsPage(Game game)
        {
            InitializeComponent();
            GameTitle.Text = game.name;
            _achievementImageFetcher = new AchievementImage();
            _ = PopulateAchievementsAsync(game);
        }

        private async Task PopulateAchievementsAsync(Game game)
        {
            var unlockedAchievements = game.achievements?.Where(a => a.unlocked).ToList() ?? new List<Achievement>();
            var lockedAchievements = game.achievements?.Where(a => !a.unlocked).ToList() ?? new List<Achievement>();



            UnlockedAchievementsHeader.Text = $"Unlocked ({unlockedAchievements.Count})";
            LockedAchievementsHeader.Text = $"Locked ({lockedAchievements.Count})";

            var unlockedTasks = unlockedAchievements.Select(PrepareAchievementDataAsync);
            var lockedTasks = lockedAchievements.Select(PrepareAchievementDataAsync);

            var unlockedDisplayData = await Task.WhenAll(unlockedTasks);
            var lockedDisplayData = await Task.WhenAll(lockedTasks);

            foreach (var data in unlockedDisplayData)
            {
                UnlockedAchievementsList.Items.Add(CreateAchievementItem(data));
            }

            foreach (var data in lockedDisplayData)
            {
                LockedAchievementsList.Items.Add(CreateAchievementItem(data));
            }
        }

        private async Task<AchievementDisplayData> PrepareAchievementDataAsync(Achievement achievement)
        {
            return await Task.Run(() =>
            {
                var processedImage = _achievementImageFetcher.GetProcessedImage(achievement.imageUrl, achievement.unlocked, achievement.hidden);
                var bitmapImage = ConvertToBitmapImage(processedImage);
                bitmapImage.Freeze();
                return new AchievementDisplayData
                {
                    OriginalAchievement = achievement,
                    ProcessedImage = bitmapImage
                };
            });
        }

        private Border CreateAchievementItem(AchievementDisplayData data)
        {
            var achievement = data.OriginalAchievement;

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 59, 76)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var image = new System.Windows.Controls.Image
            {
                Source = data.ProcessedImage,
                Width = 64,
                Height = 64,
                Margin = new Thickness(0, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(image);

            var detailsStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (!achievement.unlocked && achievement.hidden)
            {
                detailsStack.Children.Add(new TextBlock
                {
                    Text = "Hidden Achievement",
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.White)
                });
                detailsStack.Children.Add(new TextBlock
                {
                    Text = "Hold to unlock",
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
                    TextWrapping = TextWrapping.Wrap
                });
            }
            else
            {
                detailsStack.Children.Add(new TextBlock
                {
                    Text = achievement.name,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.White)
                });
                detailsStack.Children.Add(new TextBlock
                {
                    Text = achievement.description,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
                    TextWrapping = TextWrapping.Wrap
                });
                if (achievement.progress)
                {
                    var progressGrid = new Grid
                    {
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    var progressBar = new ProgressBar
                    {
                        Maximum = achievement.maxProgress,
                        Value = achievement.currentProgress,
                        Height = 18
                    };
                    progressGrid.Children.Add(progressBar);

                    var progressText = new TextBlock
                    {
                        Text = $"{achievement.currentProgress} / {achievement.maxProgress}",
                        Foreground = new SolidColorBrush(Colors.White),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    progressGrid.Children.Add(progressText);

                    detailsStack.Children.Add(progressGrid);
                }
                if (achievement.unlocked)
                {
                    DateTime dateTime = StringtoDateTime(achievement.datetimeunlocked);
                    var relativeTimeText = new TextBlock
                    {
                        Text = GetRelativeTime(dateTime),
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    relativeTimeText.MouseEnter += (s, e) =>
                    {
                        relativeTimeText.Text = dateTime.ToString("dd MMMM, yyyy HH:mm");
                    };
                    relativeTimeText.MouseLeave += (s, e) =>
                    {
                        relativeTimeText.Text = GetRelativeTime(dateTime);
                    };

                    Grid.SetColumn(relativeTimeText, 2);
                    grid.Children.Add(relativeTimeText);
                }
            }
            Grid.SetColumn(detailsStack, 1);
            grid.Children.Add(detailsStack);

            border.Child = grid;
            return border;
        }

        private BitmapImage ConvertToBitmapImage(Image<Rgba32> image)
        {
            if (image == null) return null;

            using var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        private DateTime StringtoDateTime(string datetime)
        {
            if (string.IsNullOrEmpty(datetime))
            {
                return DateTime.MinValue;
            }

            string cleanedDatetime = datetime.Replace("Unlocked ", "").Replace(" @ ", " ");

            var formats = new[]
            {
                "MMM d, yyyy h:mmtt",
                "MMM d h:mmtt",
                "yyyy-MM-dd HH:mm:ss"
            };

            if (DateTime.TryParseExact(cleanedDatetime, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            if (DateTime.TryParse(datetime, out parsedDate))
            {
                return parsedDate;
            }

            return DateTime.MinValue; // Return a default value if all parsing fails
        }

        private string GetRelativeTime(DateTime? dateTime)
        {
            if (dateTime == null || dateTime == DateTime.MinValue) return "Unknown";

            var timeSpan = DateTime.Now - dateTime.Value;

            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";

            return "Just now";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}