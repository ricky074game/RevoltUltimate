using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class GameAchievementsPage : UserControl
    {
        public event EventHandler BackClicked;
        private readonly AchievementImage _achievementImageFetcher;

        public GameAchievementsPage(Game game)
        {
            InitializeComponent();
            GameTitle.Text = game.name;

            _achievementImageFetcher = new AchievementImage();

            PopulateAchievements(game);
        }

        private void PopulateAchievements(Game game)
        {
            var unlockedAchievements = game.achievements?.Where(a => a.unlocked).ToList();
            var lockedAchievements = game.achievements?.Where(a => !a.unlocked).ToList();

            UnlockedAchievementsHeader.Text = $"Unlocked ({unlockedAchievements?.Count ?? 0})";
            LockedAchievementsHeader.Text = $"Locked ({lockedAchievements?.Count ?? 0})";

            foreach (var achievement in unlockedAchievements ?? Enumerable.Empty<Achievement>())
            {
                UnlockedAchievementsList.Items.Add(CreateAchievementItem(achievement));
            }

            foreach (var achievement in lockedAchievements ?? Enumerable.Empty<Achievement>())
            {
                LockedAchievementsList.Items.Add(CreateAchievementItem(achievement));
            }
        }

        private Border CreateAchievementItem(Achievement achievement)
        {
            var border = new Border
            {
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 59, 76)),
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

            var processedImage = _achievementImageFetcher.GetProcessedImage(achievement.imageUrl, achievement.unlocked, achievement.hidden);

            if (processedImage != null)
            {
                var bitmapImage = ConvertToBitmapImage(processedImage);
                var image = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                    Width = 64,
                    Height = 64,
                    Margin = new Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                grid.Children.Add(image);
            }
            else
            {
                var bitmapImage = ConvertToBitmapImage(processedImage);

                var image = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                    Width = 64,
                    Height = 64,
                    Margin = new Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                grid.Children.Add(image);
            }


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
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                });
                detailsStack.Children.Add(new TextBlock
                {
                    Text = "Hold to unlock",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
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
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                });
                detailsStack.Children.Add(new TextBlock
                {
                    Text = achievement.description,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
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
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
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
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)),
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };

                    // Add hover events to dynamically change the text
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
                throw new ArgumentException("The datetime string cannot be null or empty.", nameof(datetime));
            }

            string cleanedDatetime = datetime.Replace("Unlocked ", "").Replace(" @ ", " ");

            var customFormat = "MMM d, yyyy h:mmtt";
            if (DateTime.TryParseExact(cleanedDatetime,
                    customFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedDate))
            {
                return parsedDate;
            }

            customFormat = "MMM d h:mmtt";
            if (DateTime.TryParseExact(cleanedDatetime,
                    customFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out parsedDate))
            {
                return parsedDate;
            }

            if (DateTime.TryParse(datetime, out parsedDate))
            {
                return parsedDate;
            }

            customFormat = "yyyy-MM-dd HH:mm:ss";
            if (DateTime.TryParseExact(datetime, customFormat, null, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                return parsedDate;
            }

            throw new FormatException($"The datetime string '{datetime}' is not in a valid format.");
        }

        private string GetRelativeTime(DateTime? dateTime)
        {
            if (dateTime == null) return "Unknown";

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