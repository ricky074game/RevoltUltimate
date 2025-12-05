using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Options
{
    public partial class DataCacheOptionsPage : UserControl
    {
        private static readonly string ImageCacheDirectory = Path.Combine(Path.GetTempPath(), "RevoltUltimateImageCache");
        private static readonly string AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate");

        private const double AverageImageSizeKb = 69.69; // Nice

        private CancellationTokenSource? _downloadCancellationToken;
        private bool _isDownloading;
        public DataCacheOptionsPage()
        {
            InitializeComponent();
            Loaded += DataCacheOptionsPage_Loaded;
        }
        private void DataCacheOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAllCacheSizes();
            CalculateDownloadEstimate();
        }
        private void RefreshAllCacheSizes()
        {
            var (imageCacheSize, imageCacheCount) = GetDirectorySize(ImageCacheDirectory);
            ImageCacheSizeText.Text = FormatSize(imageCacheSize);
            ImageCacheCountText.Text = $"{imageCacheCount} files";

            var (appDataSize, appDataCount) = GetDirectorySize(AppDataDirectory);
            AppDataSizeText.Text = FormatSize(appDataSize);
            AppDataCountText.Text = $"{appDataCount} files";

            long totalSize = imageCacheSize + appDataSize;
            TotalCacheSizeText.Text = FormatSize(totalSize);
        }
        private static (long size, int count) GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return (0, 0);

            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                long totalSize = 0;
                foreach (var file in files)
                {
                    try
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // ignored
                    }
                }
                return (totalSize, files.Length);
            }
            catch
            {
                return (0, 0);
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void RefreshCacheSize_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllCacheSizes();
            CalculateDownloadEstimate();
        }
        private void ClearImageCache_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to clear the achievement image cache?",
                "Clear Image Cache",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    if (Directory.Exists(ImageCacheDirectory))
                    {
                        Directory.Delete(ImageCacheDirectory, true);
                        Directory.CreateDirectory(ImageCacheDirectory);
                    }
                    RefreshAllCacheSizes();
                    CalculateDownloadEstimate();
                    Trace.WriteLine("Image cache cleared.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to clear image cache: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearAllCache_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you want to clear ALL cache?",
                    "Clear All Cache",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                if (Directory.Exists(ImageCacheDirectory))
                {
                    Directory.Delete(ImageCacheDirectory, true);
                    Directory.CreateDirectory(ImageCacheDirectory);
                }
                RefreshAllCacheSizes();
                CalculateDownloadEstimate();
                Trace.WriteLine("All cache cleared.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear cache: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(AppDataDirectory))
                {
                    Directory.CreateDirectory(AppDataDirectory);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppDataDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDownloadEstimate()
        {
            var allImageUrls = GetAllAchievementImageUrls();
            int totalImages = allImageUrls.Count;
            int alreadyCached = CountCachedImages(allImageUrls);

            double estimatedSizeMb = (totalImages - alreadyCached) * AverageImageSizeKb / 1024;

            TotalImagesText.Text = totalImages.ToString();
            EstimatedSizeText.Text = $"{estimatedSizeMb:F1} MB";
            AlreadyCachedText.Text = alreadyCached.ToString();
        }

        private List<string> GetAllAchievementImageUrls()
        {
            var urls = new List<string>();

            if (App.CurrentUser?.Games == null)
                return urls;

            foreach (var game in App.CurrentUser.Games)
            {
                foreach (var achievement in game.achievements)
                {
                    if (!string.IsNullOrEmpty(achievement.imageUrl) &&
                        (achievement.imageUrl.StartsWith("http://") || achievement.imageUrl.StartsWith("https://")))
                    {
                        urls.Add(achievement.imageUrl);
                    }
                }
            }

            return urls.Distinct().ToList();
        }

        private int CountCachedImages(List<string> urls)
        {
            if (!Directory.Exists(ImageCacheDirectory))
                return 0;

            int count = 0;
            foreach (var url in urls)
            {
                try
                {
                    string fileName = Path.GetFileName(new Uri(url).LocalPath);
                    string cachedPath = Path.Combine(ImageCacheDirectory, fileName);
                    if (File.Exists(cachedPath))
                        count++;
                }
                catch
                {
                    // ignored
                }
            }
            return count;
        }

        private async void StartImageDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
                return;

            var allUrls = GetAllAchievementImageUrls();
            if (allUrls.Count == 0)
            {
                MessageBox.Show("No achievement images to download!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var urlsToDownload = allUrls.Where(url =>
            {
                try
                {
                    string fileName = Path.GetFileName(new Uri(url).LocalPath);
                    string cachedPath = Path.Combine(ImageCacheDirectory, fileName);
                    return !File.Exists(cachedPath);
                }
                catch { return true; }
            }).ToList();

            if (urlsToDownload.Count == 0)
            {
                MessageBox.Show("All images are already cached!", "Already Done", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isDownloading = true;
            _downloadCancellationToken = new CancellationTokenSource();
            StartDownloadButton.IsEnabled = false;
            CancelDownloadButton.Visibility = Visibility.Visible;
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;

            int successCount = 0;
            int failedCount = 0;
            int totalToDownload = urlsToDownload.Count;

            if (!Directory.Exists(ImageCacheDirectory))
                Directory.CreateDirectory(ImageCacheDirectory);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                for (int i = 0; i < urlsToDownload.Count; i++)
                {
                    if (_downloadCancellationToken.Token.IsCancellationRequested)
                        break;

                    string url = urlsToDownload[i];

                    try
                    {
                        string fileName = Path.GetFileName(new Uri(url).LocalPath);
                        string cachedPath = Path.Combine(ImageCacheDirectory, fileName);

                        var imageData = await httpClient.GetByteArrayAsync(url, _downloadCancellationToken.Token);
                        await File.WriteAllBytesAsync(cachedPath, imageData, _downloadCancellationToken.Token);
                        successCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                    int progress = (int)((i + 1) * 100.0 / totalToDownload);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DownloadProgressBar.Value = progress;
                        DownloadProgressPercent.Text = $"{progress}%";
                        SuccessCountText.Text = successCount.ToString();
                        FailedCountText.Text = failedCount.ToString();
                        RemainingCountText.Text = (totalToDownload - i - 1).ToString();
                        DownloadStatusText.Text = $"Downloading... ({i + 1}/{totalToDownload})";
                    });
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    DownloadStatusText.Text = _downloadCancellationToken.Token.IsCancellationRequested ? "Download cancelled" : $"Complete! {successCount} downloaded, {failedCount} failed";
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Download error: {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    DownloadStatusText.Text = $"Error: {ex.Message}";
                });
            }
            finally
            {
                _isDownloading = false;
                _downloadCancellationToken?.Dispose();
                _downloadCancellationToken = null;

                await Dispatcher.InvokeAsync(() =>
                {
                    StartDownloadButton.IsEnabled = true;
                    StartDownloadButton.Content = "Start Download";
                    CancelDownloadButton.Visibility = Visibility.Collapsed;
                    RefreshAllCacheSizes();
                    CalculateDownloadEstimate();
                });
            }
        }
        private void CancelImageDownload_Click(object sender, RoutedEventArgs e)
        {
            _downloadCancellationToken?.Cancel();
        }

        private int _wipeClickCount = 0;
        private Window? _wipeConfirmWindow;

        private void WipeData_Click(object sender, RoutedEventArgs e)
        {
            _wipeClickCount++;

            switch (_wipeClickCount)
            {
                case 1:
                    var result1 = MessageBox.Show(
                        "Are you sure you want to wipe ALL data?\n\n",
                        "Delete Everything?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result1 != MessageBoxResult.Yes)
                    {
                        _wipeClickCount = 0;
                        return;
                    }

                    WipeDataButton.Content = "Are you REALLY sure?";
                    break;

                case 2:
                    var result2 = MessageBox.Show(
                        "Okay, you clicked yes again...\n\n" +
                        "This is PERMANENT. Like, really permanent.\n" +
                        "Like \"your ex's number after they cheated\" permanent.",
                        "Last Chance... Or Is It?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Exclamation);

                    if (result2 != MessageBoxResult.Yes)
                    {
                        _wipeClickCount = 0;
                        WipeDataButton.Content = "Wipe All Data";
                        return;
                    }

                    ShowFinalWipeConfirmation();
                    break;
            }
        }

        private void ShowFinalWipeConfirmation()
        {
            _wipeConfirmWindow = new Window
            {
                Title = "Final Confirmation",
                Width = 500,
                Height = 300,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };

            var mainBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27)),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(24)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var titleText = new TextBlock
            {
                Text = "FINAL WARNING",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(titleText, 0);
            mainGrid.Children.Add(titleText);
            var messageText = new TextBlock
            {
                Text = "Alright, you absolute madlad.\n\n" +
                       "This is it. The point of no return.\n",
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(messageText, 1);
            mainGrid.Children.Add(messageText);
            var cancelButton = new Button
            {
                Content = "Nevermind, I want my data",
                Padding = new Thickness(20, 10, 20, 10),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 39, 42)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            cancelButton.Click += (s, args) =>
            {
                _wipeClickCount = 0;
                WipeDataButton.Content = "Wipe All Data";
                _wipeConfirmWindow?.Close();
                _wipeConfirmWindow = null;
            };
            Grid.SetRow(cancelButton, 2);
            mainGrid.Children.Add(cancelButton);

            mainBorder.Child = mainGrid;
            _wipeConfirmWindow.Content = mainBorder;
            var deleteWindow = new Window
            {
                Width = 200,
                Height = 50,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false
            };
            deleteWindow.Left = SystemParameters.PrimaryScreenWidth - 220;
            deleteWindow.Top = 20;
            var deleteBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)),
                CornerRadius = new CornerRadius(8),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var deleteText = new TextBlock
            {
                Text = "DELETE EVERYTHING",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            deleteBorder.Child = deleteText;
            deleteBorder.MouseLeftButtonUp += async (s, args) =>
            {
                deleteWindow.Close();
                _wipeConfirmWindow?.Close();
                _wipeConfirmWindow = null;
                await PerformDataWipe();
            };

            deleteWindow.Content = deleteBorder;

            _wipeConfirmWindow.Closed += (s, args) =>
            {
                deleteWindow.Close();
            };

            _wipeConfirmWindow.Show();
            deleteWindow.Show();
        }

        private async Task PerformDataWipe()
        {
            try
            {
                var progressWindow = new Window
                {
                    Title = "Wiping Data...",
                    Width = 300,
                    Height = 100,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var border = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20)
                };

                var text = new TextBlock
                {
                    Text = "Burning everything...",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                border.Child = text;
                progressWindow.Content = border;
                progressWindow.Show();
                await Task.Delay(1000);
                if (Directory.Exists(ImageCacheDirectory))
                {
                    try { Directory.Delete(ImageCacheDirectory, true); } catch { }
                }
                if (Directory.Exists(AppDataDirectory))
                {
                    try { Directory.Delete(AppDataDirectory, true); } catch { }
                }
                progressWindow.Close();
                MessageBox.Show(
                    "All data has been wiped!\n\n" +
                    "The application will now close.\n" +
                    "It was nice knowing your achievements...",
                    "Goodbye!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to wipe data: {ex.Message}\n\nYour data lives another day!",
                    "God level clutch", MessageBoxButton.OK, MessageBoxImage.Error);

                _wipeClickCount = 0;
                WipeDataButton.Content = "Wipe All Data";
            }
        }
    }
}