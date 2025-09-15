using Microsoft.WindowsAPICodePack.Dialogs;
using RevoltUltimate.API.Fetcher;
using RevoltUltimate.API.Objects;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RevoltUltimate.Desktop.Windows
{
    public partial class GameSearchWindow : Window
    {
        private readonly IGDB _igdb;
        private Task? _searchTask;
        private bool _isWindowLoaded = false;

        public GameSearchWindow()
        {
            InitializeComponent();
            _igdb = new IGDB();
            this.Owner = Application.Current.MainWindow;
            this.Loaded += GameSearchWindow_Loaded;
        }

        private void GameSearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close(); // Close the window when it loses activation
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            if (_searchTask?.IsCompleted == false)
            {
                await _searchTask;
            }

            string query = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(query) || query == "Search for a game...")
            {
                ResultsListBox.ItemsSource = null;
                HideLoadingCircle();
                return;
            }

            ShowLoadingCircle();
            _searchTask = PerformSearch(query);
            await _searchTask;
            HideLoadingCircle();
        }

        private async Task PerformSearch(string query)
        {
            var results = await _igdb.SearchGamesAsync(query);
            ResultsListBox.ItemsSource = results;
        }

        private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsListBox.SelectedItem != null)
            {
                HandleGameSelection(ResultsListBox.SelectedItem);
                this.Close();
            }
        }
        private void HandleGameSelection(object selectedItem)
        {
            // Ensure the code runs on the UI thread
            this.Dispatcher.Invoke(() =>
            {
                // Use the WPF-native folder picker
                var folderDialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = "Select the folder where the game is installed"
                };

                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string selectedPath = folderDialog.FileName;

                    try
                    {
                        // Find a JSON file called achievements.json in the folder and subfolders
                        var jsonFiles = Directory.GetFiles(selectedPath, "achievements.json", SearchOption.AllDirectories);
                        if (jsonFiles.Length == 0)
                        {
                            MessageBox.Show("No achievements.json file found in the selected directory.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Read and deserialize the JSON file
                        var jsonContent = File.ReadAllText(jsonFiles[0]);
                        var steamAchievements = JsonSerializer.Deserialize<List<SteamAchievement>>(jsonContent);

                        if (steamAchievements == null || !steamAchievements.Any())
                        {
                            MessageBox.Show("The achievements.json file is empty or invalid.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Create a new Game object
                        var game = new Game
                        (
                            selectedItem is SearchResult searchResult ? searchResult.Name : "Unknown Game",
                            "PC",
                            "",
                            "",
                            "Steam Emulator",
                            steamAchievements.Select(sa => new Achievement(
                                Name: sa.DisplayName,
                                Description: sa.Description,
                                ImageUrl: Path.Combine(Path.GetDirectoryName(jsonFiles[0]) ?? string.Empty, sa.Icon),
                                Hidden: sa.Hidden == 1,
                                Id: 0,
                                Unlocked: false,
                                DateTimeUnlocked: null,
                                Difficulty: 0, // Difficulty is not available in the JSON
                                apiName: sa.Name,
                                progress: false,
                                currentProgress: 0,
                                maxProgress: 0,
                                getglobalpercentage: 0.0f
                            )).ToList(),
                            0);

                        var mainWindow = (MainWindow)Application.Current.MainWindow;
                        mainWindow.AddGame(game);

                        MessageBox.Show($"Game '{game.name}' and its {game.achievements.Count} achievements were added successfully.", "Game Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while processing the achievements.json file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });
        }

        private void ShowLoadingCircle()
        {
            LoadingCircle.Visibility = Visibility.Visible;

            var rotateAnimation = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            LoadingCircleRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void HideLoadingCircle()
        {
            LoadingCircle.Visibility = Visibility.Collapsed;
            LoadingCircleRotation.BeginAnimation(RotateTransform.AngleProperty, null); // Stop animation
        }
    }
}